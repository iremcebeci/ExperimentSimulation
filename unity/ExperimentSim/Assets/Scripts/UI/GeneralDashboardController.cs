using UnityEngine;
using UnityEngine.UIElements;

public class GeneralDashboardController : MonoBehaviour
{
    private AppRouter router;
    private VisualElement root;

    public void Bind(AppRouter appRouter, VisualElement dashboardView)
    {
        router = appRouter;
        root = dashboardView;

        Debug.Log($"[GeneralDashboardController] Bound to dashboard: {gameObject.name}");
    }
}