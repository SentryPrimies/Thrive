﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   A page in the Thriveopedia generated by content from the online wiki.
/// </summary>
public abstract class ThriveopediaWikiPage : ThriveopediaPage
{
    [Export]
    public NodePath? MainArticlePath;

#pragma warning disable CA2213
    protected PackedScene pageSectionScene = null!;
    protected VBoxContainer mainArticle = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Wiki content to display on this page.
    /// </summary>
    public GameWiki.Page PageContent { get; set; } = null!;

    /// <summary>
    ///   Link to this page in the online wiki.
    /// </summary>
    public string Url => PageContent.Url;

    public override string PageName => PageContent.InternalName;
    public override string TranslatedPageName => TranslationServer.Translate(PageContent.Name);

    /// <summary>
    ///   Creates all wiki pages using the data in wiki.json, in order of their definition. In particular, parents must
    ///   always come before children in this list.
    /// </summary>
    public static List<ThriveopediaWikiPage> GenerateAllWikiPages()
    {
        var pages = new List<ThriveopediaWikiPage>();

        var wiki = SimulationParameters.Instance.GetWiki();

        // Generate Stage Pages

        GeneratePage<ThriveopediaStagesRootPage>(pages, wiki.StagesRoot,
            "res://src/thriveopedia/pages/wiki/stage/ThriveopediaStagesRootPage.tscn");

        GeneratePages<ThriveopediaStagePage>(pages, wiki.Stages,
            "res://src/thriveopedia/pages/wiki/stage/ThriveopediaStagePage.tscn");

        // Generate Mechanics Pages

        GeneratePage<ThriveopediaMechanicsRootPage>(pages, wiki.MechanicsRoot,
            "res://src/thriveopedia/pages/wiki/mechanic/ThriveopediaMechanicsRootPage.tscn");

        GeneratePages<SimpleWikiPage>(pages, wiki.Mechanics,
            "res://src/thriveopedia/pages/SimpleWikiPage.tscn",
            p => p.Parent = "MechanicsRoot");

        // Generate Organelle Pages

        GeneratePage<ThriveopediaOrganellesRootPage>(pages, wiki.OrganellesRoot,
            "res://src/thriveopedia/pages/wiki/organelle/ThriveopediaOrganellesRootPage.tscn");

        GeneratePages<ThriveopediaOrganellePage>(pages, wiki.Organelles,
            "res://src/thriveopedia/pages/wiki/organelle/ThriveopediaOrganellePage.tscn",
            p => p.Organelle = SimulationParameters.Instance.GetOrganelleType(p.PageContent.InternalName));

        // Generate Development Pages

        GeneratePage<ThriveopediaDevelopmentRootPage>(pages, wiki.DevelopmentRoot,
            "res://src/thriveopedia/pages/wiki/development/ThriveopediaDevelopmentRootPage.tscn");

        GeneratePages<SimpleWikiPage>(pages, wiki.DevelopmentPages,
            "res://src/thriveopedia/pages/SimpleWikiPage.tscn",
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

    /// <summary>
    ///   Adds a page section to the main content container in the scene.
    /// </summary>
    protected void AddSection(GameWiki.Page.Section content)
    {
        var section = (WikiPageSection)pageSectionScene.Instance();

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
            }
        }

        base.Dispose(disposing);
    }

    private static T GeneratePage<T>(List<ThriveopediaWikiPage> pageList, GameWiki.Page page, string scenePath)
        where T : ThriveopediaWikiPage
    {
        var pageScene = GD.Load<PackedScene>(scenePath);
        var pageInstance = (T)pageScene.Instance();
        pageInstance.PageContent = page;
        pageList.Add(pageInstance);

        return pageInstance;
    }

    private static void GeneratePages<T>(List<ThriveopediaWikiPage> pageList, List<GameWiki.Page> pages,
        string scenePath, Action<T>? extraDataInit = null)
        where T : ThriveopediaWikiPage
    {
        var pageScene = GD.Load<PackedScene>(scenePath);

        foreach (var page in pages)
        {
            var pageInstance = (T)pageScene.Instance();
            pageInstance.PageContent = page;

            if (page.NoticeSceneName != null)
            {
                var noticeScene = GD.Load<PackedScene>($"res://src/thriveopedia/pages/notices/{page.NoticeSceneName}.tscn");
                var noticeInstance = noticeScene.Instance();
                var noticeContainer = pageInstance.GetNode<VBoxContainer>(pageInstance.NoticeContainerPath);
                noticeContainer.AddChild(noticeInstance);
            }

            extraDataInit?.Invoke(pageInstance);
            pageList.Add(pageInstance);
        }
    }
}
