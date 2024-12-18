﻿using GooeyWpf.Transcriber;

namespace GooeyWpf.Commands
{
    public class CommandManager
    {
        private readonly List<Command> commands = new();
        private readonly ITranscriber transcriber;
        private readonly string wakeCommand;
        private readonly string[] variations;
        private bool listening = false;
        private bool wakeResponded = false;

        public CommandManager(ITranscriber transcriber, string wakeCommand, string[] variations)
        {
            this.transcriber = transcriber;
            this.wakeCommand = wakeCommand;
            this.variations = variations;
            transcriber.Transcribe += Transcriber_Transcribe;
        }

        public event EventHandler<ITranscriber.TranscribeEventArgs>? Transcribe;

        public event EventHandler? Wake;

        public event EventHandler? Sleep;

        public void RegisterCommand(Command command)
        {
            command.OriginalTranscribeEvent = Transcriber_Transcribe;
            commands.Add(command);
        }

        public void RegisterCommands(IEnumerable<Command> commands)
        {
            foreach (var command in commands)
            {
                RegisterCommand(command);
            }
        }

        public void UnregisterCommands()
        {
            foreach (var command in commands)
            {
                command.OriginalTranscribeEvent -= Transcriber_Transcribe;
                commands.Remove(command);
            }
        }

        public void Stop()
        {
            transcriber.Transcribe -= Transcriber_Transcribe;
        }

        private int FirstIndexFrom(string findFrom, string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                int index = findFrom.IndexOf(strings[i]);
                if (index >= 0) return index;
            }
            return -1;
        }

        private void Transcriber_Transcribe(object? sender, ITranscriber.TranscribeEventArgs eventArgs)
        {
            string text = eventArgs.Text.ToLower();

            //foreach (var variation in variations)
            //{
            //    if (Common.RemovePunctuation(text).Contains(variation, StringComparison.CurrentCultureIgnoreCase))
            //    {
            //        listening = true;
            //        break;
            //    }
            //}

            string remaining = "";
            if (!listening)
            {
                foreach (var variation in variations)
                {
                    string textDumb = Common.RemovePunctuation(text);
                    if (textDumb.Contains(variation, StringComparison.CurrentCultureIgnoreCase))
                    {
                        remaining = textDumb.Replace(variation.ToLower(), "").Trim();
                        listening = true;
                        break;
                    }
                }
                remaining = remaining.Trim();
            }

            Transcribe?.Invoke(sender, eventArgs);
            if (listening)
            {
                if (string.IsNullOrEmpty(remaining))
                {
                    if (!wakeResponded)
                    {
                        Wake?.Invoke(this, EventArgs.Empty);
                        wakeResponded = true;
                    }
                    remaining = text;
                }

                foreach (var command in commands)
                {
                    if (command.CommandMatch(remaining))
                    {
                        command.Parse(remaining);
                        listening = false;
                        wakeResponded = false;
                        Sleep?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                }
            }
        }
    }
}