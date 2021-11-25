/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using PFS.Shared.Types;
using PFS.Shared.Stalker;

namespace PFS.Shared.StalkerAddons
{
    // Extends base StalkerAction/StalkerContent API w 'manual commandline API that allows short commands & expands API w additional functionalities'
    public class StalkerCmdLine
    {
        protected StalkerContent _stalkerContent = null; // Uses Content that is actual master parser/performer..

        public StalkerCmdLine(StalkerContent stalkerContent)
        {
            _stalkerContent = stalkerContent;
            _stalkerContent.TrackActions();
        }

        // !!!LATER!!! Add more extensions, helps etc to make usage of CmdLine more descriptive.. not urgent... 

        public StalkerError DoCmdLine(string cmdLine, out List<string> output)
        {
            output = null;

            //
            // Hardcoded system commands
            //
            switch ( cmdLine.ToUpper() )
            {
                case "LIST":                    // Returns current 'actions' list
                    {
                        output = new();
                        List<string> actions = _stalkerContent.GetActions();
                        foreach ( string action in actions )
                        {
                            output.Add(string.Format("{0}", action));
                        }
                    }
                    return StalkerError.OK;
            }

            // 
            // Try to see if one of 'StalkerContent' commands speed version
            // 
            StalkerError standardError = DoStandardAction(cmdLine);

            if (standardError == StalkerError.OK )
                return StalkerError.OK;

            //
            // Looks like this is not one of standard Stalker's Operation+Element's.. so lets see if extension command
            //
            StalkerError extensionError = DoExtensionAction(cmdLine, out output);

            if (extensionError == StalkerError.OK)
                return StalkerError.OK;

            // Here is bit questionable what error to return.. as both failed, but atm well stick to main Stalker errors with some exceptions
            switch ( standardError )
            {
                case StalkerError.Unknown:
                case StalkerError.NotSupported:
                    return extensionError;
            }
            return standardError;
        }

        // This function provides a speed command version of 'StalkerContent' API functionalities, without any extension operations/combos
        protected StalkerError DoStandardAction(string cmdLine)
        {
            List<string> cmdSegments = StalkerSplit.SplitLine(cmdLine);
            cmdSegments.RemoveAll(s => string.IsNullOrWhiteSpace(s) == true);

            if (cmdSegments.Count == 0)
                return StalkerError.FAIL;

            // First segment's first character is ALWAYS a operation 
            StalkerOperation stalkerOperation = LocalParseStalkerOperation(cmdSegments[0]);

            if (stalkerOperation == StalkerOperation.Unknown)
                // That didnt go too far...
                return StalkerError.Unknown;

            // Rest of first segment is element that is operatation targets
            StalkerElement stalkerElement = LocalParseElement(cmdSegments[0]);

            if (stalkerElement == StalkerElement.Unknown)
                return StalkerError.Unknown;

            // we do create this here, to allow some early (double) verifications and specially allow some DEF -field handlings those dont belong main content
            StalkerAction verifyStalkerAction = StalkerAction.Create(stalkerOperation, stalkerElement);

            if (verifyStalkerAction == null)
                // This where all those commands are supposed to fail, if not part of core StalkerContent but example a Extension by CmdLine
                return StalkerError.NotSupported;

            if (verifyStalkerAction.Parameters.Count() != cmdSegments.Count() - 1)
                return StalkerError.MissingParameters;

            // Got speed command part, ala shortcut Operation-Element parsed.. so recreate less speedy formatted command line
            StringBuilder builder = new();
            builder.Append(stalkerOperation.ToString() + "-");
            builder.Append(stalkerElement.ToString() + " ");

            int segmentID = 0;
            foreach ( string segment in cmdSegments )
            {
                segmentID++;

                if (segmentID == 1 )
                    // Always skip first, as thats speed commands thats already done w Operation-Elem
                    continue;

                StalkerParam param = verifyStalkerAction.Parameters[segmentID - 2];

                switch (param.Type )
                {
                    // This is special parameter type, only CmdLine supports 'ticker/search' functionality.. as Content requires STID as Guid
                    case StalkerParam.ParamType.STID:
                        {
                            Guid STID = GetSTID(segment);

                            if (STID == Guid.Empty)
                                return StalkerError.UnTrackedStock;

                            builder.Append(param.Name + "=[" + STID.ToString() + "] ");
                        }
                        continue;

                    case StalkerParam.ParamType.HoldingID:
                        {
                            if (segment == "_")
                            {
                                // We send empty parameter in, to gain default functionality that is PFS system generated HoldingID
                                builder.Append(param.Name + "=[] ");
                            }
                            else
                                builder.Append(param.Name + "=[" + segment + "] ");
                        }
                        continue;

                    default:

                        builder.Append(param.Name + "=[" + segment + "] ");
                        continue;
                }
            }

            return _stalkerContent.DoAction(builder.ToString().TrimEnd());


            StalkerOperation LocalParseStalkerOperation(string cmd)
            {
                Tuple<char, StalkerOperation, CmdLineOperation> tuple = SupportedOperations.Where(o => o.Item1 == cmd.ToUpper().First()).SingleOrDefault();

                if (tuple == null)
                    return StalkerOperation.Unknown;

                return tuple.Item2;
            }

            StalkerElement LocalParseElement(string cmd)
            {
                if (string.IsNullOrWhiteSpace(cmd) == true )
                    return StalkerElement.Unknown;

                Tuple<string, StalkerElement, CmdLineElement> tuple = SupportedElements.Where(t => t.Item1 == cmd.Substring(1).ToUpper()).SingleOrDefault();

                if (tuple == null)
                    return StalkerElement.Unknown;

                return tuple.Item2;
            }
        }

        protected StalkerError DoExtensionAction(string cmdLine, out List<string> output)
        {
            output = null;

            List<string> cmdSegments = StalkerSplit.SplitLine(cmdLine);
            cmdSegments.RemoveAll(s => string.IsNullOrWhiteSpace(s) == true);

            if (cmdSegments.Count == 0)
                return StalkerError.FAIL;

            // First segment's first character is ALWAYS a operation 
            CmdLineOperation cmdLineOperation = LocalParseCmdLineOperation(cmdSegments[0]);

            if (cmdLineOperation == CmdLineOperation.Unknown)
                // That didnt go too far...
                return StalkerError.Unknown;

            // Rest of first segment is element that is operatation targets
            CmdLineElement cmdLineElement = LocalParseCmdLineElement(cmdSegments[0]);

            if (cmdLineElement == CmdLineElement.Unknown)
                return StalkerError.Unknown;

            output = new();

            switch (cmdLineElement)
            {
                case CmdLineElement.Alarm:

                    switch ( cmdLineOperation )
                    {
                        case CmdLineOperation.List:     // LA Stock
                            {
                                List<StalkerParam> parameters;
                                StalkerError error = LocalParseParameters(out parameters);
                                if (error != StalkerError.OK)
                                    return error;

                                Stock stock = _stalkerContent.StockRef((Guid)parameters[0]);

                                if (stock == null)
                                    return StalkerError.UnTrackedStock;

                                foreach ( StockAlarm alarm in stock.Alarms.OrderBy(o => o.Value) )
                                {
                                    switch ( alarm.Type )
                                    {
                                        case StockAlarmType.Over:
                                        case StockAlarmType.Under:

                                            output.Add(string.Format("{0} {1} {2}", alarm.Value.ToString("0.00"), alarm.Type, alarm.Note));
                                            break;

                                        default:

                                            output.Add(string.Format("{0} {1} {2} {3}", alarm.Value.ToString("0.00"), alarm.Type, alarm.Param1, alarm.Note));
                                            break;
                                    }
                                }
                                return StalkerError.OK;
                            }
                    }
                    break;

                case CmdLineElement.AlarmUnder:

                    switch (cmdLineOperation)
                    {
                        case CmdLineOperation.Add:      // AAU Stock Value Note
                            {                           // ->  Add-Alarm Type=Under Param1=0.0 Stock Value  Note
                                List<StalkerParam> parameters;
                                StalkerError error = LocalParseParameters(out parameters);
                                if (error != StalkerError.OK)
                                    return error;

                                return LocalDoAction(
                                    "Add-Alarm",
                                    "Type=Under Param1=0.01", 
                                    parameters);
                            }

                        case CmdLineOperation.Edit:     // EAU Stock EditedValue Value Note
                            {                           // ->  Add-Alarm Type=Under Param1=0.01 Stock EditedValue Value  Note
                                List<StalkerParam> parameters;
                                StalkerError error = LocalParseParameters(out parameters);
                                if (error != StalkerError.OK)
                                    return error;

                                return LocalDoAction(
                                    "Edit-Alarm",
                                    "Type=Under Param1=0.01",
                                    parameters);
                            }
                    }
                    break;

                case CmdLineElement.AlarmOver: 

                    switch (cmdLineOperation)
                    {
                        case CmdLineOperation.Add:      // AAO Stock Value Note
                            {                           // ->  Add-Alarm Type=Over Param1=0.0 Stock Value  Note
                                List<StalkerParam> parameters;
                                StalkerError error = LocalParseParameters(out parameters);
                                if (error != StalkerError.OK)
                                    return error;

                                return LocalDoAction(
                                    "Add-Alarm",
                                    "Type=Over Param1=0.01",
                                    parameters);
                            }
                    }
                    break;

                case CmdLineElement.AlarmUnderWatchP:
                case CmdLineElement.AlarmUnderWatch2P:
                case CmdLineElement.AlarmUnderWatch3P:
                case CmdLineElement.AlarmUnderWatch4P:
                case CmdLineElement.AlarmUnderWatch5P:
                case CmdLineElement.AlarmUnderWatch6P:
                case CmdLineElement.AlarmUnderWatch7P:
                case CmdLineElement.AlarmUnderWatch8P:
                case CmdLineElement.AlarmUnderWatch9P:
                    {
                        decimal procent = 5.0M; // too high and so early in may catch nasty drops too easy.. too low and misses few days drop strike.. try 5%

                        switch (cmdLineElement)
                        {
                            case CmdLineElement.AlarmUnderWatch2P: procent = 2; break;
                            case CmdLineElement.AlarmUnderWatch3P: procent = 3; break;
                            case CmdLineElement.AlarmUnderWatch4P: procent = 4; break;
                            case CmdLineElement.AlarmUnderWatch5P: procent = 5; break;
                            case CmdLineElement.AlarmUnderWatch6P: procent = 6; break;
                            case CmdLineElement.AlarmUnderWatch7P: procent = 7; break;
                            case CmdLineElement.AlarmUnderWatch8P: procent = 8; break;
                            case CmdLineElement.AlarmUnderWatch9P: procent = 9; break;
                        }

                        switch (cmdLineOperation)
                        {
                            case CmdLineOperation.Add:      // AAxx Stock Value Note
                                {                           // ->  Add-Alarm Type=Under Param1=0.0 Stock Value  Note
                                    List<StalkerParam> parameters;
                                    StalkerError error = LocalParseParameters(out parameters);
                                    if (error != StalkerError.OK)
                                        return error;

                                    return LocalDoAction(
                                        "Add-Alarm",
                                        "Type=UnderWatchP " + string.Format("Param1={0}", procent),
                                        parameters);
                                }
                        }
                    }
                    break;

                case CmdLineElement.AlarmOverWatchP:
                    {
                        decimal procent = 5.0M; // too high and so early in may catch nasty drops too easy.. too low and misses few days drop strike.. try 5%

                        switch (cmdLineOperation)
                        {
                            case CmdLineOperation.Add:      // AAxx Stock Value Note
                                {                           // ->  Add-Alarm Type=Over Param1=0.0 Stock Value  Note
                                    List<StalkerParam> parameters;
                                    StalkerError error = LocalParseParameters(out parameters);
                                    if (error != StalkerError.OK)
                                        return error;

                                    return LocalDoAction(
                                        "Add-Alarm",
                                        "Type=OverWatchP " + string.Format("Param1={0}", procent),
                                        parameters);
                                }
                        }
                    }
                    break;

                case CmdLineElement.AlarmUnderWatch:

                    switch (cmdLineOperation)
                    {
                        case CmdLineOperation.Add:      // AAUW Stock Value Watch Note
                            {                           // ->  Add-Alarm Type=UnderWatch Stock Value Param1 Note 
                                List<StalkerParam> parameters;
                                StalkerError error = LocalParseParameters(out parameters);
                                if (error != StalkerError.OK)
                                    return error;

                                return LocalDoAction(
                                    "Add-Alarm",
                                    "Type=UnderWatch",
                                    parameters);
                            }

                        case CmdLineOperation.Edit:     // AAUW Stock EditedValue Value Watch Note
                            {                           // ->  Add-Alarm Type=UnderWatch Stock EditedValue Value Param1 Note 
                                List<StalkerParam> parameters;
                                StalkerError error = LocalParseParameters(out parameters);
                                if (error != StalkerError.OK)
                                    return error;

                                return LocalDoAction(
                                    "Edit-Alarm",
                                    "Type=UnderWatch",
                                    parameters);
                            }
                    }
                    break;
            }

            return StalkerError.FAIL;

            CmdLineOperation LocalParseCmdLineOperation(string cmd)
            {
                Tuple<char, StalkerOperation, CmdLineOperation> tuple = SupportedOperations.Where(o => o.Item1 == cmd.ToUpper().First()).SingleOrDefault();

                if (tuple == null)
                    return CmdLineOperation.Unknown;

                return tuple.Item3;
            }

            CmdLineElement LocalParseCmdLineElement(string cmd)
            {
                if (string.IsNullOrWhiteSpace(cmd) == true)
                    return CmdLineElement.Unknown;

                Tuple<string, StalkerElement, CmdLineElement> tuple = SupportedElements.Where(t => t.Item1 == cmd.Substring(1).ToUpper()).SingleOrDefault();

                if (tuple == null)
                    return CmdLineElement.Unknown;

                return tuple.Item3;
            }

            StalkerError LocalParseParameters(out List<StalkerParam> parameters)
            {
                parameters = new();
                string[] expectedParameters = StalkerCmdLineTemplate.Get(cmdLineOperation, cmdLineElement);

                if (expectedParameters == null || expectedParameters.Count() == 0)
                    // This operation is not supported
                    return StalkerError.NotSupported;

                foreach (string paramTemplate in expectedParameters)
                {
                    // And create all parameter templates, so after creation its ready to accept parameters
                    parameters.Add(new StalkerParam(paramTemplate));
                }

                if (parameters.Count() != cmdSegments.Count() - 1)
                    return StalkerError.MissingParameters;

                int segmentID = 0;
                foreach ( string segment in cmdSegments )
                {
                    segmentID++;

                    if (segmentID == 1)
                        continue;

                    StalkerError error;
                    StalkerParam param = parameters[segmentID - 2];

                    if ( param.Type == StalkerParam.ParamType.STID )
                    {
                        Guid STID = GetSTID(segment);
                        if (STID == Guid.Empty)
                            return StalkerError.UnTrackedStock;

                        error = param.Parse(STID.ToString());
                    }
                    else
                        error = param.Parse(segment);

                    if (error != StalkerError.OK)
                        return error;
                }

                // Validate just in case all got set
                foreach (StalkerParam param in parameters)
                    if (param.Error != StalkerError.OK)
                        return StalkerError.MissingParameters;

                return StalkerError.OK;
            }

            StalkerError LocalDoAction(string combo, string fixedParams, List<StalkerParam> parameters)
            {
                /* LocalDoAction
                 *   takes string as command
                 *   parameters as Name=Value so order doesnt matter
                 *   plus string w fixed Name=Value parameters
                 *   calls DoAction w composed cmd line
                 */

                StringBuilder builder = new();
                builder.Append(combo + " ");
                builder.Append(fixedParams + " ");

                foreach (StalkerParam param in parameters )
                    // Add each parsed parameter as string as dont wanna care types.. 
                    builder.Append(param.Name + "=[" + (string)param + "] ");

                return _stalkerContent.DoAction(builder.ToString());
            }
        }

        protected Guid GetSTID(string search)
        {
            // 1) $ticker
            // 2) ticker
            // 3) search !!!LATER!!!
            // => With help of StockMeta we do search, and convert this to actual Guid STID that StalkerContent wants!

            if (search.StartsWith('$') == true)
                search = search.Substring(1);

            List<Stock> stocks = _stalkerContent.Stocks();

            Stock stock = stocks.FirstOrDefault(s => s.Meta.Ticker.ToUpper() == search.ToUpper());

            if (stock != null)
                return stock.Meta.STID;

            return Guid.Empty;
        }

        // First letter of speed command
        private readonly static ImmutableArray<Tuple<char, StalkerOperation, CmdLineOperation>> SupportedOperations = ImmutableArray.Create(new Tuple<char, StalkerOperation, CmdLineOperation>[]
        {
            new Tuple<char, StalkerOperation, CmdLineOperation>('?', StalkerOperation.Unknown,      CmdLineOperation.Help),
            new Tuple<char, StalkerOperation, CmdLineOperation>('L', StalkerOperation.Unknown,      CmdLineOperation.List),
            new Tuple<char, StalkerOperation, CmdLineOperation>('A', StalkerOperation.Add,          CmdLineOperation.Add),
            new Tuple<char, StalkerOperation, CmdLineOperation>('E', StalkerOperation.Edit,         CmdLineOperation.Edit),
            new Tuple<char, StalkerOperation, CmdLineOperation>('D', StalkerOperation.Delete,       CmdLineOperation.Delete),
            new Tuple<char, StalkerOperation, CmdLineOperation>('M', StalkerOperation.Move,         CmdLineOperation.Move),
            new Tuple<char, StalkerOperation, CmdLineOperation>('W', StalkerOperation.DeleteAll,    CmdLineOperation.DeleteAll),
          //new Tuple<char, StalkerOperation, CmdLineOperation>('S', StalkerOperation.Set,          CmdLineOperation.Set), => 'S' trying to avoid adding it here! As SET is special commands w optional parameters.. and doesnt really fit in nor give value here!
            new Tuple<char, StalkerOperation, CmdLineOperation>('T', StalkerOperation.Top,          CmdLineOperation.Top),
            new Tuple<char, StalkerOperation, CmdLineOperation>('F', StalkerOperation.Follow,       CmdLineOperation.Follow),
            new Tuple<char, StalkerOperation, CmdLineOperation>('U', StalkerOperation.Unfollow,     CmdLineOperation.Unfollow),
        });

        // Second and following letters of speed command (until first space)
        private readonly static ImmutableArray<Tuple<string, StalkerElement, CmdLineElement>> SupportedElements = ImmutableArray.Create(new Tuple<string, StalkerElement, CmdLineElement>[]
        {
            new Tuple<string, StalkerElement, CmdLineElement>("?",      StalkerElement.Unknown,                 CmdLineElement.Help),

            // Adding stocks from command line, maybe dont allow this thru here! its async operation to get that info... 
            //new Tuple<string, StalkerElement, CmdLineElement>("S?",     StalkerElement.Unknown,                 CmdLineElement.StockHelp),
            //new Tuple<string, StalkerElement, CmdLineElement>("S",      StalkerElement.Stock,                   CmdLineElement.Base),

            new Tuple<string, StalkerElement, CmdLineElement>("P?",     StalkerElement.Unknown,                 CmdLineElement.PortfolioHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("P",      StalkerElement.Portfolio,               CmdLineElement.Portfolio),

            new Tuple<string, StalkerElement, CmdLineElement>("G?",     StalkerElement.Unknown,                 CmdLineElement.GroupHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("G",      StalkerElement.Group,                   CmdLineElement.Group),

            new Tuple<string, StalkerElement, CmdLineElement>("H?",     StalkerElement.Unknown,                 CmdLineElement.HoldingHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("H",      StalkerElement.Holding,                 CmdLineElement.Holding),

            new Tuple<string, StalkerElement, CmdLineElement>("T?",     StalkerElement.Unknown,                 CmdLineElement.TradeHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("T",      StalkerElement.Trade,                   CmdLineElement.Trade),

            new Tuple<string, StalkerElement, CmdLineElement>("O?",     StalkerElement.Unknown,                 CmdLineElement.OrderHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("O",      StalkerElement.Order,                   CmdLineElement.Order),

            new Tuple<string, StalkerElement, CmdLineElement>("D?",     StalkerElement.Unknown,                 CmdLineElement.DividentHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("D",      StalkerElement.Divident,                CmdLineElement.Divident),

            

            new Tuple<string, StalkerElement, CmdLineElement>("A?",     StalkerElement.Unknown,                 CmdLineElement.AlarmHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("A",      StalkerElement.Alarm,                   CmdLineElement.Alarm),
            new Tuple<string, StalkerElement, CmdLineElement>("AU?",    StalkerElement.Unknown,                 CmdLineElement.AlarmHelp),
            new Tuple<string, StalkerElement, CmdLineElement>("AU",     StalkerElement.Unknown,                 CmdLineElement.AlarmUnder),

            new Tuple<string, StalkerElement, CmdLineElement>("AUW",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch),

            new Tuple<string, StalkerElement, CmdLineElement>("AU%",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatchP),
            new Tuple<string, StalkerElement, CmdLineElement>("AUP",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatchP),
            new Tuple<string, StalkerElement, CmdLineElement>("AU2",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch2P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU3",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch3P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU4",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch4P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU5",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch5P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU6",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch6P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU7",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch7P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU8",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch8P),
            new Tuple<string, StalkerElement, CmdLineElement>("AU9",    StalkerElement.Unknown,                 CmdLineElement.AlarmUnderWatch9P),

            new Tuple<string, StalkerElement, CmdLineElement>("AO",     StalkerElement.Unknown,                 CmdLineElement.AlarmOver),
            new Tuple<string, StalkerElement, CmdLineElement>("AO%",     StalkerElement.Unknown,                CmdLineElement.AlarmOverWatchP),
            new Tuple<string, StalkerElement, CmdLineElement>("AOP",     StalkerElement.Unknown,                CmdLineElement.AlarmOverWatchP),
        });

        // Going to need somekind of own template and/or allowed combo functionality... as above ones allow all combos.. anyway
        // can trust StalkerAction for any Base combo checkings.. but maybe small table for 'valid CmdLine extensions'
    }

    internal enum CmdLineOperation : int // Additionals to StalkerOperation (Add/Edit/ReplaceAll/Delete), are:
    {
        Unknown = 0,
        Add,                // Core StalkerOperation
        Edit,               // Core StalkerOperation
        Delete,             // Core StalkerOperation
        Move,               // Core StalkerOperation
        DeleteAll,          // Core StalkerOperation
        Set,                // Core StalkerOperation
        Top,                // Core StalkerOperation
        Follow,             // Core StalkerOperation
        Unfollow,           // Core StalkerOperation

        Help,               // Extends to offer textual help functionality              !!!LATER!!! Implement
        List,               // Extends to see current list of specific elements         !!!LATER!!! Implement
    }

    internal enum CmdLineElement : int // Additionals to StalkerElement (
    {
        Unknown = 0,

        Portfolio,          // Core StalkerElement
        Group,              // Core StalkerElement
        Stock,              // Core StalkerElement
        Holding,            // Core StalkerElement
        Alarm,              // Core StalkerElement
        Order,              // Core StalkerElement
        Trade,              // Core StalkerElement
        Divident,           // Core StalkerElement

        Help,
        StockHelp,
        PortfolioHelp,
        GroupHelp,
        HoldingHelp,
        TradeHelp,
        OrderHelp,
        DividentHelp,
        AlarmHelp,
        AlarmOver,
        AlarmOverWatchP,
        AlarmUnder,
        AlarmUnderWatch,
        AlarmUnderWatchP,
        AlarmUnderWatch2P,
        AlarmUnderWatch3P,
        AlarmUnderWatch4P,
        AlarmUnderWatch5P,
        AlarmUnderWatch6P,
        AlarmUnderWatch7P,
        AlarmUnderWatch8P,
        AlarmUnderWatch9P,
    }
}
