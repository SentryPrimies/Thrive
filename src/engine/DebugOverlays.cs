﻿using Godot;

/// <summary>
///   Main script for debugging.
///   Partial class: Override functions, debug panel
/// </summary>
public partial class DebugOverlays : Control
{
    [Export]
    public NodePath? DebugCoordinatesPath;

    [Export]
    public NodePath FPSCheckBoxPath = null!;

    [Export]
    public NodePath PerformanceMetricsCheckBoxPath = null!;

    [Export]
    public NodePath InspectorCheckboxPath = null!;

    [Export]
    public NodePath NetworkCheckboxPath = null!;

    [Export]
    public NodePath DebugPanelDialogPath = null!;

    [Export]
    public NodePath FPSCounterPath = null!;

    [Export]
    public NodePath PerformanceMetricsPath = null!;

    [Export]
    public NodePath NetworkDebugPath = null!;

    [Export]
    public NodePath EntityLabelsPath = null!;

    [Export]
    public NodePath InspectorDialogPath = null!;

    private static DebugOverlays? instance;

#pragma warning disable CA2213
    private Label debugCoordinates = null!;
    private CustomWindow inspectorDialog = null!;
    private CustomWindow debugPanelDialog = null!;
    private CustomCheckBox fpsCheckBox = null!;
    private CustomCheckBox performanceMetricsCheckBox = null!;
    private CustomCheckBox inspectorCheckbox = null!;
    private CustomCheckBox networkCheckbox = null!;
    private Control fpsCounter = null!;
    private CustomWindow performanceMetrics = null!;
    private CustomWindow networkDebug = null!;
    private Control labelsLayer = null!;
#pragma warning restore CA2213

    private Rect2? reportedViewportSize;

    private DebugOverlays()
    {
        instance = this;
    }

    public static DebugOverlays Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        base._Ready();

        debugCoordinates = GetNode<Label>(DebugCoordinatesPath);
        inspectorDialog = GetNode<CustomWindow>(InspectorDialogPath);
        inspectorCheckbox = GetNode<CustomCheckBox>(InspectorCheckboxPath);
        fpsCheckBox = GetNode<CustomCheckBox>(FPSCheckBoxPath);
        performanceMetricsCheckBox = GetNode<CustomCheckBox>(PerformanceMetricsCheckBoxPath);
        debugPanelDialog = GetNode<CustomWindow>(DebugPanelDialogPath);
        fpsCounter = GetNode<Control>(FPSCounterPath);
        performanceMetrics = GetNode<CustomWindow>(PerformanceMetricsPath);
        networkDebug = GetNode<CustomWindow>(NetworkDebugPath);
        networkCheckbox = GetNode<CustomCheckBox>(NetworkCheckboxPath);
        labelsLayer = GetNode<Control>(EntityLabelsPath);
        smallerFont = GD.Load<Font>("res://src/gui_common/fonts/Lato-Regular-Tiny.tres");
        fpsLabel = GetNode<Label>(FPSLabelPath);
        deltaLabel = GetNode<Label>(DeltaLabelPath);
        metricsText = GetNode<Label>(MetricsTextPath);
        networkMetricsText = GetNode<Label>(NetworkMetricsTextPath);
        fpsDisplayLabel = GetNode<Label>(FPSDisplayLabelPath);

        networkCheckbox.Pressed = true;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Show();
        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InputManager.UnregisterReceiver(this);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (inspectorDialog.Visible)
            UpdateInspector();

        // Entity label
        if (showEntityLabels)
            UpdateEntityLabels();

        // Performance metrics
        if (performanceMetrics.Visible)
            UpdateMetrics(delta);

        // Network metrics
        if (networkDebug.Visible)
            UpdateNetworkDebug(delta);

        // FPS counter
        if (fpsCounter.Visible)
            UpdateFPS();

        // Parts of the game that aren't the GUI may want to know the actual logical size of our window (for example to
        // check mouse coordinates), so this seems like a sensible place to do that as there's no longer a general
        // overlay manager class
        var size = GetViewportRect();

        if (reportedViewportSize != size)
        {
            GUICommon.Instance.ReportViewportRect(size);
            reportedViewportSize = size;
        }
    }

    [RunOnKeyDown("toggle_metrics", OnlyUnhandled = false)]
    public void OnPerformanceMetricsToggled()
    {
        performanceMetricsCheckBox.Pressed = !performanceMetricsCheckBox.Pressed;
    }

    public void OnNetworkDebugToggled()
    {
        networkCheckbox.Pressed = !networkCheckbox.Pressed;
    }

    [RunOnKeyDown("toggle_debug_panel", OnlyUnhandled = false)]
    public void OnDebugPanelToggled()
    {
        if (!debugPanelDialog.Visible)
        {
            debugPanelDialog.Show();
        }
        else
        {
            debugPanelDialog.Hide();
        }
    }

    [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false)]
    public void OnFpsToggled()
    {
        fpsCheckBox.Pressed = !fpsCheckBox.Pressed;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (DebugCoordinatesPath != null)
            {
                DebugCoordinatesPath.Dispose();
                FPSCheckBoxPath.Dispose();
                FPSLabelPath.Dispose();
                DeltaLabelPath.Dispose();
                MetricsTextPath.Dispose();
                InspectorDialogPath.Dispose();

                PerformanceMetricsCheckBoxPath.Dispose();
                InspectorCheckboxPath.Dispose();
                NetworkCheckboxPath.Dispose();
                DebugPanelDialogPath.Dispose();
                FPSCounterPath.Dispose();
                PerformanceMetricsPath.Dispose();
                NetworkDebugPath.Dispose();
                EntityLabelsPath.Dispose();
                FPSDisplayLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnPerformanceMetricsCheckBoxToggled(bool state)
    {
        if (performanceMetrics.Visible == state)
            return;

        if (state)
        {
            performanceMetrics.Show();
        }
        else
        {
            performanceMetrics.Hide();
        }
    }

    private void OnNetworkDebugCheckBoxToggled(bool state)
    {
        if (networkDebug.Visible == state)
            return;

        if (state)
        {
            networkDebug.Show();
            NetworkManager.Instance.InitProfiling();
        }
        else
        {
            networkDebug.Hide();
            NetworkManager.Instance.ClearProfiling();
        }
    }

    private void OnFpsCheckBoxToggled(bool state)
    {
        fpsCounter.Visible = state;
    }

    private void OnCollisionShapeCheckBoxToggled(bool state)
    {
        GetTree().DebugCollisionsHint = state;
    }

    private void OnEntityLabelCheckBoxToggled(bool state)
    {
        if (showEntityLabels == state)
            return;

        ShowEntityLabels = state;

        if (state)
        {
            InitiateEntityLabels();
        }
        else
        {
            CleanEntityLabels();
        }
    }

    private void OnTransparencySliderValueChanged(float value)
    {
        performanceMetrics.Modulate = debugPanelDialog.Modulate = new Color(1, 1, 1, 1 - value);
    }

    private void OnDumpSceneTreeButtonPressed()
    {
        DumpSceneTreeToFile(GetTree().Root);
    }
}
