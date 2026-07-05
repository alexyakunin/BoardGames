using System.Security;
using BoardGames.Abstractions;
using Xunit;

namespace BoardGames.Tests;

public class ChatServiceTest : IClassFixture<TestAppHostFixture>
{
    private readonly TestAppHost _host;

    public ChatServiceTest(TestAppHostFixture fixture)
        => _host = fixture.Host;

    private IServiceProvider ClientServices => _host.ClientServices;
    private ICommander ClientCommander => ClientServices.Commander();

    [Fact]
    public async Task PostAndReadMessages()
    {
        var session = await _host.SignIn("Chatter");
        var chats = ClientServices.GetRequiredService<IChatService>();

        var game = await ClientCommander.Call(new Game_Create(session, "gomoku"));
        var chatId = Chat.GetGameChatId(game.Id);

        var chatMessage = await ClientCommander.Call(new Chat_Post(session, chatId, "Hello from the test!"));
        Assert.NotNull(chatMessage);

        var messageCount = await chats.GetMessageCount(chatId);
        Assert.Equal(1, messageCount);

        var tail = await chats.GetTail(session, chatId, 10);
        var message = Assert.Single(tail.Messages);
        Assert.Equal("Hello from the test!", message.Text);
        Assert.True(tail.Users.ContainsKey(message.UserId));
    }

    [Fact]
    public async Task MessageCountInvalidation()
    {
        var session = await _host.SignIn("ChatWatcher");
        var chats = ClientServices.GetRequiredService<IChatService>();

        var game = await ClientCommander.Call(new Game_Create(session, "gomoku"));
        var chatId = Chat.GetGameChatId(game.Id);

        // Capture the client-side computed for the message count
        var computed = await Computed.Capture(() => chats.GetMessageCount(chatId));
        Assert.Equal(0, computed.Value);

        // Post a message & check the captured computed gets invalidated over RPC
        await ClientCommander.Call(new Chat_Post(session, chatId, "Invalidation test"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await computed.WhenInvalidated(cts.Token);

        computed = await computed.Update();
        Assert.Equal(1, computed.Value);
    }

    [Fact]
    public async Task AnonymousUsersCannotPost()
    {
        var ownerSession = await _host.SignIn("ChatOwner");
        var game = await ClientCommander.Call(new Game_Create(ownerSession, "gomoku"));
        var chatId = Chat.GetGameChatId(game.Id);

        var anonymousSession = Session.New();
        await Assert.ThrowsAnyAsync<Exception>(
            () => ClientCommander.Call(new Chat_Post(anonymousSession, chatId, "Should fail")));
    }
}
