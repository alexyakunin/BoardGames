using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ActualLab.Serialization.Internal;
using ActualLab.Trimming;
using BoardGames.UI;

// Commands that "return nothing" complete with a System.Reactive.Unit result, whose
// MessagePack formatter DefaultMessagePackResolver instantiates reflectively. Full trimming
// can't see that hop, so it drops the formatter's ctor and every authenticated command then
// fails to deserialize its result - keep the ctor explicitly.
if (CodeKeeper.AlwaysFalse) {
    CodeKeeper.Keep<UnitMessagePackFormatter>();
}

try {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    ClientStartup.ConfigureServices(builder.Services, builder);
    var host = builder.Build();
    StaticLog.Factory = host.Services.LoggerFactory();
    await host.RunAsync();
}
catch (Exception error) {
    // WASM surfaces only the outermost exception; log the whole chain so a boot
    // failure (e.g. a type trimmed away) is diagnosable from the browser console.
    for (var e = error; e != null; e = e.InnerException)
        Console.WriteLine($"{e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
    throw;
}
