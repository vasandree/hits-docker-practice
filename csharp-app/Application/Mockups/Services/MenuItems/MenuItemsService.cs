using Mockups.Models.Menu;
using Mockups.Storage;
using Mockups.Repositories.MenuItems;
using Mockups.Services.Carts;

namespace Mockups.Services.MenuItems
{
    public class MenuItemsService : IMenuItemsService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly MenuItemRepository _menuItemRepository;
        private readonly ICartsService _cartsService;

        private static readonly string[] AllowedExtensions = { "jpg", "jpeg", "png" };

        public MenuItemsService(IWebHostEnvironment environment, MenuItemRepository menuItemRepository, ICartsService cartsService)
        {
            _environment = environment;
            _menuItemRepository = menuItemRepository;
            _cartsService = cartsService;
        }

        public async Task CreateMenuItem(CreateMenuItemViewModel model)
        {
            var sameMenuItem = await _menuItemRepository.GetItemByName(model.Name);
            if (sameMenuItem != null)
            {
                throw new ArgumentException($"Menu item with same name ({model.Name}) already exists");
            }

            var isFileAttached = model.File != null;
            var fileNameWithPath = string.Empty;
            if (isFileAttached)
            {
                var extension = Path.GetExtension(model.File!.FileName).Replace(".", "");
                if (!AllowedExtensions.Contains(extension))
                {
                    throw new ArgumentException("Attached file's extention is not supported");
                }
                fileNameWithPath = BuildPhotoPath(model.File.FileName);
                using (var fs = new FileStream(Path.Combine(_environment.WebRootPath, fileNameWithPath), FileMode.Create))
                {
                    await model.File.CopyToAsync(fs);
                }
            }

            var newMenuItem = new MenuItem
            {
                Name = model.Name,
                Price = model.Price,
                Description = model.Description,
                Category = model.Category,
                IsVegan = model.IsVegan,
                PhotoPath = fileNameWithPath
            };

            await _menuItemRepository.AddItem(newMenuItem);
        }

        public async Task<List<MenuItemViewModel>> GetAllMenuItems(bool? isVegan, MenuItemCategory[]? category)
        {
            var itemVMs = new List<MenuItemViewModel>();
            var items = new List<MenuItem>();

            var hasCategories = category?.Any() == true;
            if (isVegan != null && hasCategories)
            {
                items = await _menuItemRepository.GetAllMenuItems((bool)isVegan, category);
            }
            else if (hasCategories)
            {
                items = await _menuItemRepository.GetAllMenuItems(category);
            }
            else if (isVegan != null)
            {
                items = await _menuItemRepository.GetAllMenuItems((bool)isVegan);
            }
            else
            {
                items = await _menuItemRepository.GetAllMenuItems();
            }
            foreach (var item in items)
            {
                itemVMs.Add(BuildMenuItemViewModel(item));
            }

            return itemVMs;
        }

        public async Task<bool?> DeleteMenuItem(string id)
        {
            var guid = Guid.Parse(id);

            var item = await _menuItemRepository.GetItemById(guid);

            if (item == null)
            {
                return null;
            }

            await _menuItemRepository.DeleteItem(item);

            return true;
        }

        public async Task<MenuItemViewModel?> GetItemModelById(string id)
        {
            var guid = Guid.Parse(id);

            var item = await _menuItemRepository.GetItemById(guid);

            if (item == null)
            {
                return null;
            }

            return BuildMenuItemViewModel(item);
        }

        public async Task AddItemToCart(Guid userID, string itemId, int amount)
        {
            await _cartsService.AddItemToCart(userID, itemId, amount);
        }

        public async Task<AddToCartViewModel> GetAddToCartModel(string itemId)
        {
            var itemGuid = Guid.Parse(itemId);

            var item = await _menuItemRepository.GetItemById(itemGuid);

            if (item == null)
            {
                throw new KeyNotFoundException();
            }

            return new AddToCartViewModel
            {
                Item = BuildMenuItemViewModel(item),
            };
        }

        public async Task<string?> GetItemNameById(Guid itemId)
        {
            return (await _menuItemRepository.GetItemById(itemId))?.Name;
        }

        private static string BuildPhotoPath(string fileName)
        {
            return $"files/{Guid.NewGuid()}-{fileName}";
        }

        private static MenuItemViewModel BuildMenuItemViewModel(MenuItem item)
        {
            return new MenuItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Category = item.Category,
                IsVegan = item.IsVegan,
                PhotoPath = item.PhotoPath
            };
        }
    }
}
