using Newtonsoft.Json;
using PagedList;
using Sample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sample.Controllers
{
    public class HotSpotController : Controller
    {
        /// <summary>
        /// Indexes the specified page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="districts">The districts.</param>
        /// <param name="types">The types.</param>
        /// <param name="companys">The companys.</param>
        /// <returns></returns>
        public async Task<ActionResult> Index(
            int? page, 
            string districts, 
            string types, 
            string companys)
        {
            //區域
            ViewBag.Districts =
                this.GetSelectList(await this.GetDistricts(), districts);
            ViewBag.SelectedDistrict = districts;

            //熱點分類
            var typeSelectList =
                this.GetSelectList(await this.GetHotSpotTypes(), types);
            ViewBag.Types = typeSelectList.ToList();
            ViewBag.SelectedType = types;

            //業者
            var companySelectList =
                this.GetSelectList(await this.GetCompanys(), companys);

            ViewBag.Companys = companySelectList.ToList();
            ViewBag.SelectedCompany = companys;

            var source = await this.GetHotSpotData();
            source = source.AsQueryable();

            if (!string.IsNullOrWhiteSpace(districts))
            {
                source = source.Where(x => x.District == districts);
            }
            if (!string.IsNullOrWhiteSpace(types))
            {
                source = source.Where(x => x.Type == types);
            }
            if (!string.IsNullOrWhiteSpace(companys))
            {
                source = source.Where(x => x.Company == companys);
            }

            int pageIndex = page ?? 1;
            int pageSize = 10;
            int totalCount = 0;

            totalCount = source.Count();

            source = source.OrderBy(x => x.District)
                           .Skip((pageIndex - 1) * pageSize)
                           .Take(pageSize);

            var pagedResult = new StaticPagedList<HotSpot>(source, pageIndex, pageSize, totalCount);

            return View(pagedResult);
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
        /// Gets the hot spot data.
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<HotSpot>> GetHotSpotData()
        {
            const string cacheName = "WIFI_HOTSPOT";

            ObjectCache cache = MemoryCache.Default;
            var cacheContents = cache.GetCacheItem(cacheName);

            if (cacheContents == null)
            {
                return await RetriveHotSpotData(cacheName);
            }
            else
            {
                return cacheContents.Value as IEnumerable<HotSpot>;
            }
        }

        /// <summary>
        /// Retrives the hot spot data.
        /// </summary>
        /// <param name="cacheName">Name of the cache.</param>
        /// <returns></returns>
        private async Task<IEnumerable<HotSpot>> RetriveHotSpotData(string cacheName)
        {
            const string targetUri = "http://data.ntpc.gov.tw/NTPC/od/data/api/IMC123/?$format=json";

            var client = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue
            };
            var response = await client.GetStreamAsync(targetUri);

            var collection = 
                JsonConvert.DeserializeObject<IEnumerable<HotSpot>>(response.ToString());

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(30)
            };

            ObjectCache cacheItem = MemoryCache.Default;
            cacheItem.Add(cacheName, collection, policy);

            return collection;
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

    }
}