using Elsa.Labels.Services;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Get;

public class Get : Endpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Get(ILabelStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/labels/{id}");
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = await _store.FindByIdAsync(request.Id, cancellationToken);

        if (label == null)
            await SendNotFoundAsync(cancellationToken);
        else
        {
            var response = await Map.FromEntityAsync(label);
            await SendOkAsync(response, cancellationToken);
        }
    }
}