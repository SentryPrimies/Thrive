﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   A page in the Thriveopedia generated by content from the online wiki.
/// </summary>
[GodotAbstract]
public partial class ThriveopediaWikiPage : ThriveopediaPage, IThriveopediaPage
{
    [Export]
    public NodePath? MainArticlePath;

    [Export]
    public NodePath NoticeContainerPath = null!;

#pragma warning disable CA2213
    protected PackedScene pageSectionScene = null!;
    protected VBoxContainer mainArticle = null!;
#pragma warning restore CA2213

    protected ThriveopediaWikiPage()
    {
    }

    public bool VisibleInTree { get; set; } = true;

    /// <summary>
    ///   Wiki content to display on this page.
    /// </summary>
    public GameWiki.Page PageContent { get; set; } = null!;

    /// <summary>
    ///   Link to this page in the online wiki.
    /// </summary>
    public string Url => PageContent.Url;

    public virtual string PageName => PageContent.InternalName;
    public virtual string TranslatedPageName => Localization.Translate(PageContent.Name);

    public virtual string? ParentPageName => null;

    /// <summary>
    ///   Creates all wiki pages using the data in wiki.json, in order of their definition. In particular, parents must
    ///   always come before children in this list.
    /// </summary>
    public static List<ThriveopediaWikiPage> GenerateAllWikiPages()
    {
        var pages = new List<ThriveopediaWikiPage>();

        var wiki = SimulationParameters.Instance.GetWiki();

        // Generate Mechanics Pages

        GeneratePage<ThriveopediaMechanicsRootPage>(pages, wiki.MechanicsRoot,
            "res://src/thriveopedia/pages/wiki/mechanics/ThriveopediaMechanicsRootPage.tscn");

        GeneratePages<ThriveopediaSimpleWikiPage>(pages, wiki.Mechanics,
            "res://src/thriveopedia/pages/ThriveopediaSimpleWikiPage.tscn",
            p => p.Parent = "MechanicsRoot");

        // Generate Stage Pages

        GeneratePages<ThriveopediaStagePage>(pages, wiki.Stages,
            "res://src/thriveopedia/pages/wiki/stages/ThriveopediaStagePage.tscn");

        // Generate Organelle Pages

        GeneratePage<ThriveopediaOrganellesRootPage>(pages, wiki.OrganellesRoot,
            "res://src/thriveopedia/pages/wiki/organelles/ThriveopediaOrganellesRootPage.tscn");

        GeneratePages<ThriveopediaOrganellePage>(pages, wiki.Organelles,
            "res://src/thriveopedia/pages/wiki/organelles/ThriveopediaOrganellePage.tscn",
            p => p.Organelle = SimulationParameters.Instance.GetOrganelleType(p.PageContent.InternalName));

        // Generate Development Pages

        GeneratePage<ThriveopediaDevelopmentRootPage>(pages, wiki.DevelopmentRoot,
            "res://src/thriveopedia/pages/wiki/development/ThriveopediaDevelopmentRootPage.tscn");

        GeneratePages<ThriveopediaSimpleWikiPage>(pages, wiki.DevelopmentPages,
            "res://src/thriveopedia/pages/ThriveopediaSimpleWikiPage.tscn",
            p => p.Parent = "DevelopmentRoot");

        return pages;
    }

    public override void _Ready()
    {
        base._Ready();

        mainArticle = GetNode<VBoxContainer>(MainArticlePath);
        pageSectionScene = GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/WikiPageSection.tscn");

        foreach (var section in PageContent.Sections)
            AddSection(section);
    }

    public virtual void OnSelectedStageChanged()
    {
    }

    /// <summary>
    ///   Adds a page section to the main content container in the scene.
    /// </summary>
    protected void AddSection(GameWiki.Page.Section content)
    {
        var section = pageSectionScene.Instantiate<WikiPageSection>();

        if (content.SectionHeading != null)
            section.HeadingText = content.SectionHeading;

        section.BodyText = content.SectionBody;
        mainArticle.AddChild(section);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MainArticlePath != null)
            {
                MainArticlePath.Dispose();
                pageSectionScene.Dispose();
                NoticeContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Loads a page scene and populates it with content
    /// </summary>
    private static void GeneratePage<T>(List<ThriveopediaWikiPage> pageList, GameWiki.Page page, string scenePath)
        where T : ThriveopediaWikiPage
    {
        var pageScene = GD.Load<PackedScene>(scenePath);
        var pageInstance = (T)pageScene.Instantiate();
        pageInstance.PageContent = page;
        pageList.Add(pageInstance);
    }

    /// <summary>
    ///   Loads a set of pages and populates them with content.
    ///   Additionally, performs extraDataInit for each page if needed.
    ///   Also adds a notice to the page if present.
    /// </summary>
    /// <param name="pageList">A reference to all the pages</param>
    /// <param name="pages">Pages to generate</param>
    /// <param name="scenePath">The scene to load for all the pages</param>
    /// <param name="extraDataInit">
    ///   An action to be performed on all pages. Used to pass extra data to the page.
    /// </param>
    private static void GeneratePages<T>(List<ThriveopediaWikiPage> pageList, List<GameWiki.Page> pages,
        string scenePath, Action<T>? extraDataInit = null)
        where T : ThriveopediaWikiPage
    {
        var pageScene = GD.Load<PackedScene>(scenePath);

        foreach (var page in pages)
        {
            var pageInstance = (T)pageScene.Instantiate();
            pageInstance.PageContent = page;

            if (page.NoticeSceneName != null)
            {
                var noticePath = $"res://src/thriveopedia/pages/notices/{page.NoticeSceneName}.tscn";
                var noticeScene = GD.Load<PackedScene>(noticePath);
                var noticeInstance = noticeScene.Instantiate();
                var container = pageInstance.GetNode(pageInstance.NoticeContainerPath);
                container.AddChild(noticeInstance);
            }

            extraDataInit?.Invoke(pageInstance);
            pageList.Add(pageInstance);
        }
    }
}
