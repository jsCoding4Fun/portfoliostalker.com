/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using PfsDevelUI.PFSLib;

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class TabNews
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        protected List<News> _view = null;

        protected string _newsText = string.Empty;

        protected override void OnParametersSet()
        {
            Reload();
        }

        public void Reload() // Note! Can be called also by owner
        {
            _view = PfsClientAccess.Fetch().NewsGetList().Where(n => n.Status != NewsStatus.Closed && n.Category == NewsCategory.UserNormal).ToList();
            StateHasChanged();
        }

        private void OnRowClicked(TableRowClickEventArgs<News> data)
        {
            _newsText = data.Item.Text;

            if (data.Item.Status == NewsStatus.Unread)
            {
                PfsClientAccess.Fetch().NewsChangeStatus(data.Item.ID, NewsStatus.Read);

                Reload();
            }
        }

        protected void OnBtnDelAsync(News news)
        {
            PfsClientAccess.Fetch().NewsChangeStatus(news.ID, NewsStatus.Closed);

            Reload();
        }
    }
}
