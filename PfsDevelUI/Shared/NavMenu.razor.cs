using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using PfsDevelUI;
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;
using MudBlazor;

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Shared
{
    public partial class NavMenu
    {
        // !!!STUDY!!! Drawer for popup menu: https://mudblazor.com/components/drawer

        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; } // https://blazor-university.com/routing/navigating-our-app-via-code/

        private HashSet<NavTreeElem> TreeItems { get; set; } = new HashSet<NavTreeElem>();

        protected string _clientVersion = "N/A";

        protected override void OnInitialized()
        {
            // Have little helper singleton to communicate menu selections, and allow clients to refresh menu
            PfsUiState.OnMenuUpdated += OnMenuUpdated;

            try
            {
                _clientVersion = PfsClientAccess.PfsClientVersionNumber;

                TreeItems = PfsClientAccess.GetTreeData();
            }
            catch ( Exception e )
            {
                string hmm = e.Message;
            }
        }

        public void TreeActivationChanged(NavTreeElem d)
        {
            if (d == null)
                // Gives null if selecting exactly same link twice
                return;

            switch (d.Type)
            {
                case ViewTreeEntry.Account:

                    NavigationManager.NavigateTo("/Account");
                    break;

                case ViewTreeEntry.Portfolio:

                    NavigationManager.NavigateTo("/Portfolio/" + d.Name); // forceLoad = true... goes white screen, its too strong.. dont use!
                    break;

                case ViewTreeEntry.StockGroup:

                    NavigationManager.NavigateTo("/StockGroup/" + d.Name);
                    break;
            }
        }

        protected void OnMenuUpdated()
        {
            TreeItems = PfsClientAccess.GetTreeData();
            StateHasChanged();
        }

        private bool collapseNavMenu = true;
        private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
    }
}
