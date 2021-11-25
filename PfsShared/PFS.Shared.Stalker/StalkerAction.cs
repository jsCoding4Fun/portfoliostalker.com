/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;

using PFS.Shared.Types;

namespace PFS.Shared.Stalker
{
    // Holder of Action, that is single command w Operation-Element combo plus set of parameters
    public class StalkerAction
    {
        // Each actions is defined w combo of Operation-Element.. example 'Add-Alarm' 
        public StalkerOperation Operation { get; internal set; } = StalkerOperation.Unknown;
        public StalkerElement Element { get; internal set; } = StalkerElement.Unknown;

        // And AllowedCombo, has one or multiple parameters it requires to receive (atm doesnt support zero parameters)
        public List<StalkerParam> Parameters { get; internal set; } = new();

        // Hidden constructor
        protected StalkerAction() { }

        // Always to be created with factory type method to get properly initialized, or null for failure (= not supported combo)
        public static StalkerAction Create(StalkerOperation operation, StalkerElement element)
        {
            StalkerAction ret = new();

            ret.Operation = operation;
            ret.Element = element;

            if (operation == StalkerOperation.Unknown || element == StalkerElement.Unknown)
                return null;

            // Per Operation-Element combo lets get template descripting all expected parameters for this StalkerAction
            string[] expectedParameters = StalkerActionTemplate.Get(operation, element);

            if (expectedParameters == null || expectedParameters.Count() == 0)
                // This operation is not supported
                return null;

            foreach (string paramTemplate in expectedParameters)
                // And create all parameter templates, so after creation its ready to accept parameters
                ret.Parameters.Add(new StalkerParam(paramTemplate));

            // If got this far, then its acceptable combo w now all parameters initialized w templates but not yet set
            return ret;
        }

        // Allows to check if has all parameters properly set w valid values, and ready for action
        public bool IsReady()
        {
            foreach (StalkerParam param in Parameters)
                if (param.Error != StalkerError.OK)
                    return false;

            return true;
        }

        public StalkerError SetParam(string input)
        {
            string[] splitParam = input.Split('=');

            if (splitParam.Count() != 2)
                return StalkerError.InvalidParameter;

            foreach (StalkerParam param in Parameters )
                if ( param.Name == splitParam[0] )
                    // Found correct parameter, so lets set/parse it..
                    return param.Parse(splitParam[1]);

            return StalkerError.InvalidParameter;
        }

        // Little helper to allow access parameter w parameter Name
        public StalkerParam Param(string name) // Would be possible to 'string this[string name]' -> access w action['Stock'] .. but maybe too confusing
        {
            return Parameters.Single(p => p.Name == name); 
        }
    }
}
