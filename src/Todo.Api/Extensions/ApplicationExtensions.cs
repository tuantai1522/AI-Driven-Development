namespace Todo.Api.Extensions;

public static class ApplicationExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();

        return app;
    }
}
