/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace PFS.Shared.Stalker
{
    // Terms: https://cleartax.in/g/terms/T/index
    //        https://www.investopedia.com/terms/a/asset.asp

    public enum StalkerOperation : int
    {
        Unknown = 0,                // Note! StalkerContent & StalkerAction are focused strictly to core operations Add/Edit/DeleteAll/Delete
        Add,                        //       these are functions those UI uses thru API w 'hard coded commands'. StalkerCmdLine offers a 
        Edit,                       //       extensions to these commands by adding some of its own ones.. those never come to these Content/Action!
        Delete,
        Move,
        DeleteAll,                  // Atm w Alarms, allowing remove all alarms from specific stock, used by ImportWaveAlarms to clean table before setting again
        Set,                        // Mostly special commands w optional parameters to do different extension operations
        Top,                        // Allows to edit default order of things, by pushing defined one top on list
        Follow,                     // Mainly for Follow-Group [group] [stock]
        Unfollow,  
    }

    public enum StalkerElement : int
    {
        Unknown = 0,
        Portfolio,
        Group,
        Stock,                      // Asset? Everything is atm as 'stocks' even this really supports ETFs etc.. just cant figure out generic name
        Holding,                    // Actual owning of specific Stock on PFS
        Trade,                      // PFS uses this as finalized transaction from Buy->Holding->Sale is called Trade, ala closed position for holding
        Alarm,
        Order,                      // Sell/Buy order waiting to filled up assuming price goes proper level in time order is on market
        Divident,

        // Cash

        // !!!THINK!!! Someday when figures better name to replace 'Stock' with ?? then also replace 'Group' with 'List' under elements
        // NOT-Equity, 
        // Asset, almost could use but Alarm is there w A.. and asset is kind of too generic, almost like owning it.. but its potential

    }
}
