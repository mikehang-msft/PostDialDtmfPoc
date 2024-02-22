using Azure;
using Azure.Communication;
using Azure.Communication.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api;

public static class WebApplicationExtensions
{
    public static WebApplication AddRoomsApiMappings(this WebApplication app)
    {
        app.MapPost("api/room", async (IEnumerable<string>? participants, RoomsClient roomsClient) =>
        {
            var roomsParticipants = new List<RoomParticipant>();
            foreach(var participant in participants)
            {
                roomsParticipants.Add(new RoomParticipant(new CommunicationUserIdentifier(participant))
                {
                    Role = ParticipantRole.Presenter
                });
            }

            CommunicationRoom room = await roomsClient.CreateRoomAsync(null, null, roomsParticipants);
            return Results.Ok(room);
        });

        app.MapPost("api/room/{roomId}/participants", async ([FromQuery]string roomId, [FromBody]IEnumerable<string> participants, RoomsClient roomsClient) =>
        {
            var roomsParticipants = new List<RoomParticipant>();
            foreach(var participant in participants)
            {
                roomsParticipants.Add(new RoomParticipant(new CommunicationUserIdentifier(participant))
                {
                    Role = ParticipantRole.Presenter
                });
            }
            var response = await roomsClient.AddOrUpdateParticipantsAsync(roomId, roomsParticipants);
            return Results.Ok(response);
        });

        app.MapGet("/api/room", (RoomsClient roomsClient) =>
        {
            var rooms = roomsClient.GetRoomsAsync();
            return ProcessData(rooms);
        });

        app.MapGet("/api/room/{roomId}", (string roomId, RoomsClient roomsClient) =>
        {
            var response = roomsClient.GetRoomAsync(roomId);
            return Results.Ok(response.Result);
        });

        return app;
    }

    // need this because async streams can't be used in a lambda function
    private static async IAsyncEnumerable<T> ProcessData<T>(AsyncPageable<T> data)
    where T: notnull
    {
        await foreach(var item in data)
        {
            yield return item;
        }
    }
}
