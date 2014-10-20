using Newtonsoft.Json;
using PagedList;
using Sample.Models;
using Sample.ViewModels;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sample.Controllers
{
    public class AnotherController : Controller
    {
        const string TargetUri = "http://data.ntpc.gov.tw/NTPC/od/data/api/IMC123/?$format=json";
        const string CacheName = "WIFI_HOTSPOT";

        private const int PageSize = 10;

        private Task<List<string>> Districts
        {
            get { return this.GetDistricts(); }
        }

        private Task<List<string>> HotSpotTypes
        {
            get { return this.GetHotSpotTypes(); }
        }

        private Task<List<string>> Companys
        {
            get { return this.GetCompanys(); }
        }

        public async Task<ActionResult> Index(int page = 1)
        {
            int pageIndex = page < 1 ? 1 : page;

            var source = await this.GetHotSpotData();
            source = source.AsQueryable().OrderBy(x => x.ID);

            var model = new HotSpotViewModel
            {
                SearchParameter = new HotSpotSearchModel(),
                PageIndex = pageIndex,
                Districts = this.GetSelectList(await this.Districts, ""),
                HotSpotTypes = this.GetSelectList(await this.HotSpotTypes, ""),
                Companys = this.GetSelectList(await this.Companys, ""),
                HotSpots = source.ToPagedList(pageIndex, PageSize)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Index(HotSpotViewModel model)
        {
            int pageIndex = model.PageIndex < 1 ? 1 : model.PageIndex;

            var source = await this.GetHotSpotData();
            source = source.AsQueryable();

            if (!string.IsNullOrWhiteSpace(model.SearchParameter.District))
            {
                source = source.Where(x => x.District == model.SearchParameter.District);
            }
            if (!string.IsNullOrWhiteSpace(model.SearchParameter.HotSpotType))
            {
                source = source.Where(x => x.Type == model.SearchParameter.HotSpotType);
            }
            if (!string.IsNullOrWhiteSpace(model.SearchParameter.Company))
            {
                source = source.Where(x => x.Company == model.SearchParameter.Company);
            }

            source = source.OrderBy(x => x.ID);

            var result = new HotSpotViewModel
            {
                SearchParameter = model.SearchParameter,
                PageIndex = pageIndex,
                Districts = this.GetSelectList(
                    await this.Districts,
                    model.SearchParameter.District),
                HotSpotTypes = this.GetSelectList(
                    await this.HotSpotTypes,
                    model.SearchParameter.HotSpotType),
                Companys = this.GetSelectList(
                    await this.Companys,
                    model.SearchParameter.Company),
                HotSpots = source.ToPagedList(pageIndex, PageSize)
            };

            return View(result);
        }


        /// <summary>
        /// Gets the hot spot data.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<HotSpot>> GetHotSpotData()
        {
            ObjectCache cache = MemoryCache.Default;

            if (cache.Contains(CacheName))
            {
                var cacheContents = cache.GetCacheItem(CacheName);
                return cacheContents.Value as IEnumerable<HotSpot>;
            }
            else
            {
                return await RetriveHotSpotData(CacheName);
            }
        }

        /// <summary>
        /// Retrives the hot spot data.
        /// </summary>
        /// <param name="cacheName">Name of the cache.</param>
        /// <returns></returns>
        private async Task<IEnumerable<HotSpot>> RetriveHotSpotData(string cacheName)
        {
            var client = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue
            };

            var response = await client.GetStringAsync(TargetUri);

            //=====================================================================================

            var sw = new Stopwatch();
            sw.Start();

            //使用 JSON.Net
            //var collection =
            //    JsonConvert.DeserializeObject<IEnumerable<HotSpot>>(response);

            //使用 ServiceStack.Text (速度比 JSON.Net 快)
            var collection = response.FromJson<IEnumerable<HotSpot>>();

            sw.Stop();
            var ts = sw.Elapsed;

            var elapsedTime = String.Format("{0:00}h : {1:00}m :{2:00}s .{3:000}ms",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Debug.WriteLine("RunTime: " + elapsedTime);

            //=====================================================================================

            //資料快取
            ObjectCache cacheItem = MemoryCache.Default;

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(30)
            };

            cacheItem.Add(cacheName, collection, policy);

            return collection;
        }

        /// <summary>
        /// 取得區域資料.
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetDistricts()
        {
            var source = await this.GetHotSpotData();
            if (source == null) return new List<string>();

            var districts = source.OrderBy(x => x.District)
                                  .Select(x => x.District)
                                  .Distinct();

            return districts.ToList();
        }

        /// <summary>
        /// 取得熱點分類.
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetHotSpotTypes()
        {
            var source = await this.GetHotSpotData();
            if (source == null) return new List<string>();

            var types = source.OrderBy(x => x.Type)
                              .Select(x => x.Type)
                              .Distinct();

            return types.ToList();
        }

        /// <summary>
        /// 取得業者資料.
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetCompanys()
        {
            var source = await this.GetHotSpotData();
            if (source == null) return new List<string>();

            var companys = source.OrderBy(x => x.Company)
                                 .Select(x => x.Company)
                                 .Distinct();

            return companys.ToList();
        }

        /// <summary>
        /// Gets the select list.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="selectedItem">The selected item.</param>
        /// <returns></returns>
        private List<SelectListItem> GetSelectList(
            IEnumerable<string> source,
            string selectedItem)
        {
            var selectList = source.Select(item => new SelectListItem()
            {
                Text = item,
                Value = item,
                Selected = !string.IsNullOrWhiteSpace(selectedItem)
                           &&
                           item.Equals(selectedItem, StringComparison.OrdinalIgnoreCase)
            });
            return selectList.ToList();
        }

    }
}