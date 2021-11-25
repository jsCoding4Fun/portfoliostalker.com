using System;

using Serilog;      // Serilog (not aspcore one)
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using System.Collections.Generic;
using PfsDevelUI.PFSLib;

namespace PfsDevelUI.Shared
{
    // Per: https://www.codeproject.com/Articles/1165914/Custom-Serilog-Sink-Development
    public class RecordPfsLogsSink : ILogEventSink
    {
        IFormatProvider _formatProvider;

        List<string> _pending = new();

        public RecordPfsLogsSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (PfsClientAccess.Ref != null )
            {
                if (_pending.Count > 0 )
                {
                    // Hack, as not ready for some startup events
                    foreach ( string pending in _pending )
                    {
                        PfsClientAccess.Ref.OnRecordingLogEvent(pending + " [DELAYED]");
                    }
                    _pending = null;
                }

                PfsClientAccess.Ref.OnRecordingLogEvent(logEvent.RenderMessage(_formatProvider));
            }
            // Carefull only to record until pendings are handled, as dont wanna care here if recording is on or off
            else if ( _pending != null )
            {
                _pending.Add(logEvent.RenderMessage(_formatProvider));
            }
        }
    }

    public static class RecordPfsLogsSinkExtensions
    {
        public static LoggerConfiguration RecordPfsLogsSink(
                  this LoggerSinkConfiguration loggerConfiguration,
                  IFormatProvider fmtProvider = null)
        {
            return loggerConfiguration.Sink(new RecordPfsLogsSink(fmtProvider));
        }
    }
}
