﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Blazor.Extensions.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Nethereum.TryOnBrowser.Repositories
{
    public class CodeSampleRepository
    {
        private const string _storageKey = "User samples";
        private HttpClient _httpClient;

        private List<CodeSample> _codeSamples = new List<CodeSample>();

        public LocalStorage LocalStorage { get; set; }

        public CodeSampleRepository(HttpClient httpClient, IJSRuntime runtime)
        {
            LocalStorage = new LocalStorage(runtime);
            _httpClient = httpClient;
            LoadCSharpSamples();
            LoadVbSamples();
         
        }

        public async Task LoadUserSamplesAsync()
        {
            if (LocalStorage == null) Console.WriteLine("This is nulll");
            
            if (!loadedUserSamples && LocalStorage != null)
            {
                var samples = await LocalStorage.GetItem<CodeSample[]>(_storageKey);

                if (samples != null)
                {
                   _codeSamples.AddRange(samples);
                }

                loadedUserSamples = true;
            }
        }

        public Task AddCodeSampleAsync(CodeSample codeSample)
        {
            _codeSamples.Add(codeSample);
            return SaveCustomCodeSamples();
        }

        public async Task SaveCustomCodeSamples()
        {
            await LoadUserSamplesAsync();
            await LocalStorage.SetItem(_storageKey, _codeSamples.Where(x => x.Custom).ToArray());
        }

        public async Task RemoveCodeSampleAsync(CodeSample codeSample)
        {
            await LoadUserSamplesAsync();
            if (codeSample.Custom)
            {
                _codeSamples.Remove(codeSample);
            }

            await SaveCustomCodeSamples();
        }

        public async Task<List<CodeSample>> GetCodeSamplesAsync(CodeLanguage language)
        {
            return _codeSamples.Where(x => x.Language == language).ToList();
        }
        

        public void LoadCSharpSamples()
        {
            _codeSamples.AddRange(CSharpSamples.GetSamples());
        }

        public void LoadVbSamples()
        {
            _codeSamples.AddRange(VbNetSamples.GetSamples());
        }

        private bool loadedUserSamples = false;


    }


}


//OLD github
//private const string _githubPath = "nethereum/nethereum/nethereum.playground";

//private const string _githubContentUrl =
//    "https://api.github.com/repos/Nethereum/Nethereum.Playground/contents/samples/csharp";

//private const string _githubFileUrl =
//    "https://raw.githubusercontent.com/Nethereum/Nethereum.Playground/master/samples/csharp/";

//var contents = await _httpClient.GetJsonAsync<GithubContent[]>(_githubContentUrl);
//Console.WriteLine(contents.Length);
//Console.WriteLine(contents[0].Name);


//foreach (var content in contents)
//{
//    //var code = await _httpClient.GetStringAsync(_githubFileUrl + content.Name);

//    _codeSamples.Add(new CodeSample()
//    {
//        Name = content.Name,
//       // Code = code   
//    });
//}

//return _codeSamples.ToArray();