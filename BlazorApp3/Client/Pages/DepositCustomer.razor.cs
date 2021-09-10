using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace BlazorApp3.Client.Pages
{
    public partial class DepositCustomer
    {

        private string content;
        private double cash = 0;
        private string clientToken;
        protected string divCSS = "display: none;";
        protected void DivCSS(string divCSS)
        {
            this.divCSS = divCSS;
        }
        private DotNetObjectReference<DepositCustomer> objRef;
        private async Task Cash()
        {
            try
            {
                clientToken = new string(await _httpClient.GetFromJsonAsync<char[]>("Customer/Deposit"));

                objRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("Deposit", objRef, clientToken, cash);
                DivCSS("display: block;");
            }
            catch (Exception)
            {
            }
        }

        [JSInvokable]
        public async Task Test(string test, string test2, string test3)
        {
            content = await (await _httpClient.PostAsJsonAsync<List<string>>($"Customer/{test3}", new List<string>() { test, test2 })).Content.ReadAsStringAsync();
            StateHasChanged();
        }
    }
}