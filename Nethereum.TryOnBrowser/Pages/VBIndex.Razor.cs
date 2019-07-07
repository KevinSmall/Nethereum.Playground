﻿using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nethereum.TryOnBrowser.Monaco;

namespace Nethereum.TryOnBrowser.Pages
{
    //3
    // Based on https://github.com/Suchiman/Runny all credit to him
    public class VBIndexModel : ComponentBase
    {
        protected EditorModel editorModel;

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        protected string Output { get; set; }

        [Inject] private HttpClient Client { get; set; }

        public CodeSample[] CodeSamples { get; protected set; }
        public int SelectedCodeSample { get; protected set; }

        protected override Task OnInitAsync()
        {
            CodeSamples = new VBCodeSampleRepository().GetCodeSamples();
            SelectedCodeSample = 0;

            editorModel = new EditorModel
            {
                Language = "vb",
                Script = CodeSamples[0].Code
            };

            Compiler.InitializeMetadataReferences(Client);

            return base.OnInitAsync();
        }

        public void Run()
        {
             Compiler.WhenReady(RunInternal);
        }

        public async Task CodeSampleChanged(UIChangeEventArgs evt)
        {
            SelectedCodeSample = Int32.Parse(evt.Value.ToString());
            editorModel.Script = CodeSamples[SelectedCodeSample].Code;
            await Monaco.Interop.EditorSetAsync(JSRuntime, editorModel);
        }


        public async Task RunInternal()
        {

            Output = "";

            editorModel = await Monaco.Interop.EditorGetAsync(JSRuntime, editorModel);

            Console.WriteLine("Compiling and Running code");

            var sw = Stopwatch.StartNew();

            var currentOut = Console.Out;

            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;

            try

            {

                var (success, asm, rawBytes) = Compiler.LoadSource(editorModel.Script, editorModel.Language);
                if (success)

                {
                    // well this is interesting - We can't do async task main in VB
                    // attempting to run an async function through Sub Main causes Invoke to hang
                    // until a more concrete solution is found, running async should be done through another function (RunAsync() as Task)
                    // non-async ones can run through Sub Main or RunAsync (as a function name though not as apt)
                    var assembly = AppDomain.CurrentDomain.Load(rawBytes);

                    // check if RunAsync exists, favor this over Main
                    var RunAsyncExists = (from type in assembly.GetTypes()
                                          where type.GetMethod("RunAsync") != null
                                          select type.GetMethod("RunAsync")).Any();

                    var entry = assembly.EntryPoint;
                    if (RunAsyncExists) // if RunAsync does not exist, fallback to Main
                    {
                        entry = entry.DeclaringType.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for RunAsync
                    } else
                    {
                        entry = entry.DeclaringType.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for Main
                    }
                    
                    var hasArgs = entry.GetParameters().Length > 0;
                    var result = entry.Invoke(null, hasArgs ? new object[] { new string[0] } : null);
                    if (result is Task t)
                    {
                        await t;
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Output += writer.ToString();

            if (exception != null)
            {

                Output += "\r\n" + exception.ToString();
            }

            Console.SetOut(currentOut);
            Console.WriteLine("Output " + Output);

            sw.Stop();

            Console.WriteLine("Done in " + sw.ElapsedMilliseconds + "ms");

            StateHasChanged();

        }

    }

}
