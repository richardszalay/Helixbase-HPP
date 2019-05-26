﻿using System;
using System.Web;
using Glass.Mapper.Sc;
using Helixbase.Feature.Redirects.Models;
using Helixbase.Foundation.Content.Repositories;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Pipelines.HttpRequest;

namespace Helixbase.Feature.Redirects.Pipelines
{
    public class RedirectResolver : HttpRequestProcessor
    {
        private readonly Func<IContextRepository> _contextRepositoryThunk;
        private readonly Func<IContentRepository> _contentRepositoryThunk;

        public RedirectResolver(Func<IContextRepository> contextRepositoryThunk, Func<IContentRepository> contentRepositoryThunk)
        {
            _contextRepositoryThunk = contextRepositoryThunk ?? throw new ArgumentNullException(nameof(contextRepositoryThunk));
            _contentRepositoryThunk = contentRepositoryThunk ?? throw new ArgumentNullException(nameof(contentRepositoryThunk));
        }

        public override void Process(HttpRequestArgs args)
        {
            var httpContext = HttpContext.Current;

            // TODO: Update this logic as a domain such as https://sitecore.something will not work with redirects
            if (httpContext.Request.Url.OriginalString.ToLower().Contains("/sitecore") ||
                httpContext.Request.Url.AbsolutePath.Equals("/"))
                return;
            // We don't want to redirect an item that exists in Sitecore
            if (Context.Item == null)
                Perform301Redirect();
        }

        private void Perform301Redirect()
        {
            var redirectFolder = _contentRepositoryThunk().GetItem<IRedirectFolder>(new GetItemByQueryOptions
            {
                Query = new Query($"{_contextRepositoryThunk().GetContextSiteRoot()}/*[@@templateid='{Templates.GlobalFolder.TemplateId.ToString("B").ToUpper()}']/*[@@templateid='{Templates.RedirectFolder.TemplateId.ToString("B").ToUpper()}']")
            });

            // Could also use a builder:
            // var builder = new GetItemByQueryBuilder().Query($"{_contextRepository.GetContextSiteRoot()}/*[@@templateid='{Templates.GlobalFolder.TemplateId.ToString("B").ToUpper()}']/*[@@templateid='{Templates.RedirectFolder.TemplateId.ToString("B").ToUpper()}']");

            var path = HttpContext.Current.Request.Url.LocalPath;

            if (redirectFolder == null)
                throw new NullReferenceException(Templates.ErrorMessages.NoRedirectFolder);

            foreach (var redirect in redirectFolder.Children)
            {
                if (string.IsNullOrEmpty(redirect.RequestedUrl))
                    throw new NullReferenceException(Templates.ErrorMessages.NoUrlOnItem);

                if (string.Equals(redirect.RequestedUrl, path, StringComparison.CurrentCultureIgnoreCase))
                {
                    HttpContext.Current.Response.RedirectPermanent(LinkManager.GetItemUrl(redirect.RedirectItem), true);
                }
            }
        }
    }
}