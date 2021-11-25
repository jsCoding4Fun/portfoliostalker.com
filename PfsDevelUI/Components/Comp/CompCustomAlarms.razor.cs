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
using PfsDevelUI.PFSLib;
using PfsDevelUI.Shared;
using MudBlazor;

namespace PfsDevelUI.Components
{
    public partial class CompCustomAlarms
    {
        // Requires: wwwroot/index.html to have <script src="scripts/pfsaddscript.js"></script>

        // Basics from Javascript: How to call from /scripts/name.js file a javafunction that takes parameters and returns response
        //                           -> https://code-maze.com/how-to-call-javascript-code-from-net-blazor-webassembly/


        [Inject] private IDialogService Dialog { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        protected string _editField = "function showCustom(message) { alert(message); }";


#if false // THIS WORKS w 
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync(
                    "eval",
                    "document.body.appendChild(Object.assign(document.createElement('script'), { src: 'scripts/simple.js', type: 'text/javascript' }));");
            }
        }

        await JSRuntime.InvokeVoidAsync("showSimple", "hello");
#endif

        protected async Task OnButtonRunAsync()
        {
            if (string.IsNullOrWhiteSpace(_editField) == true)
                return;

            //_editField = await _jsModule.InvokeAsync<string>("showAlert", new { Name = "John", Age = new int[] { 35, 45 } });

            try
            {
                // function showCustom(message) { alert(message); }

                await JSRuntime.InvokeVoidAsync("pfsAddScript", _editField);

                await JSRuntime.InvokeVoidAsync("showCustom", "Hello Custom Alarms!");
            }
            catch (JSException e)
            {
                string errorMessage = $"Error Message: {e.Message}";

                await Dialog.ShowMessageBox("Run failed!", errorMessage, yesText: "Ok");
            }
        }
    }
}
