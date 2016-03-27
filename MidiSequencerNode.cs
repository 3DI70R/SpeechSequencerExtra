using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Midi;
using NAudio.Wave.SampleProviders;
using ThreeDISevenZeroR.SpeechSequencer.Core;
using System.ComponentModel;

namespace ThreeDISevenZeroR.SpeechSequencer.Extra
{
    [XmlElementBinding("MidiSequencer")]
    [Description("Воспроизводит .midi файл, заменяя инструменты на звуки")]
    public class MidiSequencerNode : NoteSequencerNode<MidiSequencerNode.Params>
    {
        public class Params
        {
            [XmlAttributeBinding]
            [Description("Привязываемый канал")]
            public int Channel { get; set; } = -1;

            [XmlAttributeBinding]
            [Description("Базовая нота")]
            public int BaseNote { get; set; } = 60;

            [XmlAttributeBinding]
            [Description("Не останавливать воспроизведение звука, если получено сообщение NoteOff")]
            public bool IgnoreNoteOff { get; set; } = false;

            [XmlAttributeBinding]
            [Description("Не кэшировать значение\n" +
            "Использовать только в случае, если воспроизводимый звук меняется после каждой перемотки.")]
            public bool DoNotCache { get; set; } = false;
        }

        private class TrackState
        {
            public IList<MidiEvent> channelEvents;
            public int currentEvent;
            public int currentTick;
            public int ticksToNextEvent;

            public bool IsEnded
            {
                get
                {
                    return currentEvent >= channelEvents.Count;
                }
            }

            public List<MidiEvent> Tick()
            {
                List<MidiEvent> list = new List<MidiEvent>();
                ticksToNextEvent--;

                while (ticksToNextEvent <= 0 && !IsEnded)
                {
                    MidiEvent e = channelEvents[currentEvent++];
                    list.Add(e);

                    if (!IsEnded)
                    {
                        MidiEvent nextEvent = channelEvents[currentEvent];
                        ticksToNextEvent = (int)nextEvent.AbsoluteTime - currentTick;
                    }
                }

                currentTick++;
                return list;
            }
        }

        private class ChannelSound
        {
            public Func<ISampleProvider> sampleFactory;
            public Params parameters;
        }

        private static float[] s_noteFrequencies =
        {
            8.175f, 8.660f, 9.175f, 9.725f, 10.30f, 10.91f, 11.56f, 12.25f, 12.98f, 13.75f, 14.57f, 15.43f,
            16.35f, 17.32f, 18.35f, 19.45f, 20.60f, 21.83f, 23.12f, 24.50f, 25.96f, 27.50f, 29.14f, 30.87f,
            32.70f, 34.65f, 36.71f, 38.89f, 41.20f, 43.65f, 46.25f, 49.00f, 51.91f, 55.00f, 58.27f, 61.74f,
            65.41f, 69.30f, 73.42f, 77.78f, 82.41f, 87.31f, 92.50f, 98.00f, 103.8f, 110.0f, 116.5f, 123.5f,
            130.8f, 138.6f, 146.8f, 155.6f, 164.8f, 174.6f, 185.0f, 196.0f, 207.7f, 220.0f, 233.1f, 246.9f,
            261.6f, 277.2f, 293.7f, 311.1f, 329.6f, 349.2f, 370.0f, 392.0f, 415.3f, 440.0f, 466.2f, 493.9f,
            523.3f, 554.4f, 587.3f, 622.3f, 659.3f, 698.5f, 740.0f, 784.0f, 830.6f, 880.0f, 932.3f, 987.8f,
            1047f, 1109f, 1175f, 1245f, 1319f, 1397f, 1480f, 1568f, 1661f, 1760f, 1865f, 1976f,
            2093f, 2217f, 2349f, 2489f, 2637f, 2794f, 2960f, 3136f, 3322f, 3520f, 3729f, 3951f,
            4186f, 4435f, 4699f, 4978f, 5274f, 5588f, 5920f, 6272f, 6645f, 7040f, 7459f, 7902f,
        };

        private List<TrackState> m_tracks;
        private Dictionary<int, ChannelSound> m_sounds;
        private MidiFile m_midiFile;

        private int m_samplesPerTick;
        private int m_nextTick;

        [XmlAttributeBinding]
        [Description("Путь до .midi файла")]
        public string MidiPath { get; set; }

        public override void OnSampleRead(int sampleIndex)
        {
            if (m_nextTick <= 0)
            {
                MIDITick();
                m_nextTick = m_samplesPerTick;
            }
            else
            {
                m_nextTick--;
            }
        }
        private void MIDITick()
        {
            foreach (TrackState channel in m_tracks)
            {
                foreach (MidiEvent e in channel.Tick())
                {
                    if (e.CommandCode == MidiCommandCode.MetaEvent)
                    {
                        MetaEvent meta = (MetaEvent)e;

                        if (meta.MetaEventType == MetaEventType.SetTempo)
                        {
                            TempoEvent metaTempo = (TempoEvent)meta;
                            m_samplesPerTick = (int)(WaveFormat.SampleRate * (60.0f / (metaTempo.Tempo * m_midiFile.DeltaTicksPerQuarterNote / WaveFormat.Channels)));
                        }
                    }
                    else
                    {
                        if (e.CommandCode == MidiCommandCode.NoteOn)
                        {
                            NoteOnEvent noteEvent = (NoteOnEvent)e;
                            PlayNoteAtChannel(noteEvent.Channel, noteEvent.NoteNumber, noteEvent.Velocity);
                        }
                        else if (e.CommandCode == MidiCommandCode.NoteOff)
                        {
                            NoteEvent noteEvent = (NoteEvent)e;
                            StopNoteAtChannel(e.Channel);
                        }
                    }

                }
            }
        }

        public override bool IsFinished
        {
            get
            {
                foreach (TrackState channel in m_tracks)
                {
                    if (!channel.IsEnded)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        protected override void OnInitNewState(Context context)
        {
            base.OnInitNewState(context);
            InitMidi();
        }

        private void InitMidi()
        {
            m_midiFile = new MidiFile(MidiPath, false);
            m_sounds = new Dictionary<int, ChannelSound>();
            m_tracks = new List<TrackState>();

            for (int i = 0; i < m_midiFile.Tracks; i++)
            {
                TrackState state = new TrackState();
                state.channelEvents = m_midiFile.Events.GetTrackEvents(i);
                m_tracks.Add(state);
            }

            for (int i = 0; i < ChildCount; i++)
            {
                Params p = GetNodeParamsAt(i);
                ChannelSound sound = new ChannelSound();

                sound.parameters = p;

                Func<IAudioNode> origFactory = GetNodeAt(i);

                if (p.DoNotCache)
                {
                    sound.sampleFactory = () =>
                    {
                        IAudioNode audio = origFactory();
                        audio.InitNewState(LocalContext);

                        return audio;
                    };
                }
                else
                {
                    IAudioNode node = origFactory();
                    node.InitNewState(LocalContext);

                    sound.sampleFactory = node.CreateCachedFactory();
                }

                m_sounds[p.Channel] = sound;
            }
        }
        private void PlayNoteAtChannel(int channel, int note, int volume)
        {
            ChannelSound sound;

            if (m_sounds.TryGetValue(channel, out sound))
            {
                float noteFrequency = s_noteFrequencies[sound.parameters.BaseNote] / s_noteFrequencies[note];

                ISampleProvider sample = sound.sampleFactory().ResampleIfNeeded(WaveFormat);
                int newSampleRate = (int)(sample.WaveFormat.SampleRate * noteFrequency);

                WdlResamplingSampleProvider resampler = new WdlResamplingSampleProvider(sample, newSampleRate);
                PlaySound(resampler, channel, volume / 127.0f);
            }
        }
        private void StopNoteAtChannel(int channel)
        {
            ChannelSound sound;

            if (m_sounds.TryGetValue(channel, out sound))
            {
                if (!sound.parameters.IgnoreNoteOff)
                {
                    FadeOut(channel);
                }
            }
        }
    }
}