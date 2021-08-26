using BlazorApp3.Shared;
using Firebase.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace BlazorApp3.Client.Pages
{
	public partial class WatchCustomer
	{
		[Parameter]
		public string Id { get; set; }

		protected MovieModel movie;
		protected string movieLink;
		protected bool? canWatch;
		protected string Acomment;
		protected string content;
		protected List<CommentModel> commentList = new();

		private bool sameDevice = false;

		protected int index = 0;
		protected override async Task OnInitializedAsync()
		{
			
			if (Id == null)
			{
				_navigationManager.NavigateTo("/MovieAdmin");
			}

			movie = await _httpClient.GetFromJsonAsync<MovieModel>($"Customer/Watch/{Id}");
			
			commentList = await _httpClient.GetFromJsonAsync<List<CommentModel>>($"Customer/Comment/{Id}/{index}");
			
			sameDevice = (await _httpClient.PostAsJsonAsync<string>("Customer/UserAgent", await JS.InvokeAsync<string>("getUserAgent"))).IsSuccessStatusCode;
			
			canWatch = await _httpClient.GetFromJsonAsync<bool>($"Customer/CanWatch/{Id}");
			if (canWatch == true)
			{
				char[] tokena = await _httpClient.GetFromJsonAsync<char[]>("User/GetToken");
				string token = new string(tokena);
				try
				{
					movieLink = await new FirebaseStorage("movie2-e3c7b.appspot.com", new FirebaseStorageOptions { AuthTokenAsyncFactory = async () => await Task.FromResult(await Task.FromResult(token)), ThrowOnCancel = true, HttpClientTimeout = TimeSpan.FromHours(2) }).Child(movie.StudioId).Child(movie.MovieId).Child("Movie").GetDownloadUrlAsync();
				}
				catch
				{
				}
			}
		}
		protected async Task View()
        {
			
			await _httpClient.PostAsJsonAsync<string>("Customer/View", Id);

			while(true)
            {
				sameDevice = (await _httpClient.PostAsJsonAsync<string>("Customer/UserAgent", await JS.InvokeAsync<string>("getUserAgent"))).IsSuccessStatusCode;
				
				if(!sameDevice)
                {
					StateHasChanged();
				}
				
				await Task.Delay(150000);
			}

		}
		protected async Task Com()
		{
			CommentModel up = new CommentModel()
			{ Time = DateTime.UtcNow, MovieId = Id, CommentText = Acomment };
			content = await (await _httpClient.PostAsJsonAsync<CommentModel>("Customer/Acomment", up)).Content.ReadAsStringAsync();


			commentList.Clear();
			for (int i = 0; i <= index; i++)
			{
				commentList.AddRange(await _httpClient.GetFromJsonAsync<List<CommentModel>>($"Customer/Comment/{Id}/{i}"));
			}

		}

		protected async Task AcDisLike(string Id)
		{
			await _httpClient.PostAsJsonAsync<string>($"Customer/ac/{Id}", "DisLike");
			commentList.Clear();
			for (int i = 0; i <= index; i++)
			{
				commentList.AddRange(await _httpClient.GetFromJsonAsync<List<CommentModel>>($"Customer/Comment/{Id}/{i}"));
			}
		}

		protected async Task AcLike(string Id)
		{
			await _httpClient.PostAsJsonAsync<string>($"Customer/ac/{Id}", "Like");
			commentList.Clear();
			for (int i = 0; i <= index; i++)
			{
				commentList.AddRange(await _httpClient.GetFromJsonAsync<List<CommentModel>>($"Customer/Comment/{Id}/{i}"));
			}
		}

		protected async Task LoadMore()
		{
			index++;
			commentList.AddRange(await _httpClient.GetFromJsonAsync<List<CommentModel>>($"Customer/Comment/{Id}/{index}"));
		}
	}
}