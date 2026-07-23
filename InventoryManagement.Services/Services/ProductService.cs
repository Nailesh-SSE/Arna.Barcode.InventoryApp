using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace InventoryManagement.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<List<ProductModel>> GetDistinctProductsAsync()
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.GetAllAsync();
        var distinctProducts = products.Where(p => !p.IsDeleted)
                                       .GroupBy(p => new { p.Name, p.MakeCompanyId})
                                       .Select(g => g.First())
                                       .ToList();
        var productModels = distinctProducts.Select(p => new ProductModel
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            Unit = p.Unit,
            UnitId = p.UnitId,
            MakeCompanyId = p.MakeCompanyId,
            MakeCompany = p.MakeCompany,
            ColourId= p.ColourId,
            ColourName= p.ColourName,
            ImagePath= p.ImagePath,
        }).ToList();
        return productModels;
    }
    public async Task<List<ProductModel>> GetAllProductsAsync()
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => !p.IsDeleted);
        if(products.Count() == 0)
        {
            return new List<ProductModel>();
        }
        var getallproducts = products.Select(p => new ProductModel
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            Unit=p.Unit,
            UnitId = p.UnitId,
            MakeCompanyId = p.MakeCompanyId,
            MakeCompany = p.MakeCompany,
            ColourId=p.ColourId,
            ColourName=p.ColourName,    
            ImagePath=p.ImagePath,
        }).ToList();

        return getallproducts;
    }

    public async Task<ProductModel?> GetProductByIdAsync(int id)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var product = await productRepository.GetByIdAsync(id);

        var getproductbyid=new ProductModel
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryName,
            CreatedBy = product.CreatedBy,
            CreatedOn = product.CreatedOn,
            UpdatedBy = product.UpdatedBy,
            UpdatedOn = product.UpdatedOn,
            IsActive = product.IsActive,
            IsDeleted = product.IsDeleted,
            Unit= product.Unit,
            UnitId = product.UnitId,
            MakeCompanyId = product.MakeCompanyId,
            MakeCompany = product.MakeCompany,
            ColourName= product.ColourName,
            ColourId = product.ColourId,
            ImagePath= product.ImagePath,
        };
        return getproductbyid;
    }

    public async Task<bool> CreateProductAsync(ProductModel productModel)
    {
        try
        {
            
            if (!await IsSkuUniqueAsync(productModel.SKU))
                return false;

            if (!await IsProductNameUniqueAsync(productModel.Name,productModel.MakeCompanyId ,productModel.Id))
                return false;

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(productModel.CategoryId);
            if (category == null)
                return false;
            await GenerateProudctSkuAndSquenceNumberAsync(productModel);
            var createdProduct = new Product
                {
                    Name = productModel.Name,
                    SKU = productModel.SKU,
                    Description = productModel.Description,
                    CategoryId = productModel.CategoryId,
                    CategoryName = productModel.CategoryName,
                    CreatedBy = productModel.CreatedBy,
                    CreatedOn = productModel.CreatedOn,
                    IsActive = true,
                    IsDeleted = productModel.IsDeleted,
                    Unit = productModel.Unit,
                    UnitId = productModel.UnitId,
                    MakeCompany = productModel.MakeCompany,
                    MakeCompanyId = productModel.MakeCompanyId,
                    SerialNumber= productModel.SerialNumber,
                    ColourId= productModel.ColourId,
                    ColourName= productModel.ColourName,
                    ImagePath = productModel.ImagePath,
            };

            var productRepository = _unitOfWork.GetRepository<Product>();
            await productRepository.AddAsync(createdProduct);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(ProductModel productModel)
    {
        try
        {
            if (!await IsSkuUniqueAsync(productModel.SKU, productModel.Id))
                return false;

            if (!await IsProductNameUniqueAsync(productModel.Name, productModel.MakeCompanyId,productModel.ColourId,productModel.Id))
                return false;

            var categoryRepository = _unitOfWork.GetRepository<Category>();
            var category = await categoryRepository.GetByIdAsync(productModel.CategoryId);
            if (category == null)
                return false;
           
            await GenerateProudctSkuAndSquenceNumberAsync(productModel);
            
            var ProductRepository = _unitOfWork.GetRepository<Product>();
            var existingProduct = await ProductRepository.GetByIdAsync(productModel.Id);


            existingProduct.Name = productModel.Name;
            existingProduct.SKU = productModel.SKU;
            existingProduct.Description = productModel.Description;
            existingProduct.CategoryId = productModel.CategoryId;
            existingProduct.CategoryName = productModel.CategoryName;
            existingProduct.UpdatedBy = productModel.UpdatedBy;
            existingProduct.UpdatedOn = productModel.UpdatedOn;
            existingProduct.IsActive = productModel.IsActive;
            existingProduct.IsDeleted = productModel.IsDeleted;
            existingProduct.Unit = productModel.Unit;
            existingProduct.UnitId = productModel.UnitId;
            existingProduct.MakeCompany = productModel.MakeCompany;
            existingProduct.MakeCompanyId = productModel.MakeCompanyId; 
            existingProduct.ColourId = productModel.ColourId;
            existingProduct.ColourName = productModel.ColourName;  
            existingProduct.ImagePath = productModel.ImagePath;
            existingProduct.UpdatedBy= productModel.UpdatedBy;
            existingProduct.UpdatedOn= DateTime.Now;

            var productRepository = _unitOfWork.GetRepository<Product>();
            productRepository.Update(existingProduct);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(int id, int userid)
    {
        try
        {
            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.GetByIdAsync(id);
            if (product == null) return false;

            var inwardItemRepository = _unitOfWork.GetRepository<InwardItem>();
            var hasInwardItems = await inwardItemRepository.FindAsync(ii => ii.ProductId == id && !ii.IsDeleted);
            if (hasInwardItems.Any())
            {
                return false; 
            }

            product.IsDeleted = true;
            product.IsActive = false;
            product.UpdatedBy = userid;
            product.UpdatedOn = DateTime.Now;
            productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => p.SKU == sku && !p.IsDeleted);
        
        if (excludeId.HasValue)
            products = products.Where(p => p.Id != excludeId.Value);

        return !products.Any();
    }
    public async Task<bool> IsProductNameUniqueAsync(string name,int companyId,int colorid ,int? excludeId = null)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => p.Name.ToLower() == name.ToLower() && p.MakeCompanyId==companyId && p.ColourId==colorid &&!p.IsDeleted );

        if (excludeId.HasValue)
            products = products.Where(p => p.Id != excludeId.Value);

        return !products.Any();
    }

    public async Task<List<ProductModel>> GetProductsByCategoryAsync(int categoryId)
    {
        var productRepository = _unitOfWork.GetRepository<Product>();
        var products = await productRepository.FindAsync(p => p.CategoryId == categoryId && !p.IsDeleted);
        if(products==null)
        {
            return new List<ProductModel>();
        }
        var getproductsbycategory = products.Select(p => new ProductModel
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            CreatedBy = p.CreatedBy,
            CreatedOn = p.CreatedOn,
            UpdatedBy = p.UpdatedBy,
            UpdatedOn = p.UpdatedOn,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            Unit = p.Unit,
            UnitId = p.UnitId,
            MakeCompany = p.MakeCompany,
            MakeCompanyId = p.MakeCompanyId,
            ColourId= p.ColourId,
            ColourName= p.ColourName,
            ImagePath= p.ImagePath,

        }).ToList();
        return getproductsbycategory;
    }

    public async Task GenerateProudctSkuAndSquenceNumberAsync(ProductModel model)
    {
        var proudctRepository = _unitOfWork.GetRepository<Product>();
        var proudcts = await proudctRepository.GetAllAsync();
        var product = proudcts.Where(a => a.IsActive && !a.IsDeleted).OrderByDescending(a => a.Id).FirstOrDefault();
        model.SerialNumber = product != null ? product.SerialNumber + 1 : 0;
        model.SKU = model.MakeCompany + " " + model.Name + " (" + model.ColourName +")";
    }

    public async Task<byte[]> ExportToExcelAsync()
    {
        var productRepo = _unitOfWork.GetRepository<Product>();
        var allproducts = await productRepo.GetAllAsync();
        var products = allproducts.Where(a => !a.IsDeleted);
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("Product Report");

        // Header row
        IRow headerRow = sheet.CreateRow(0);
        string[] headers = new string[]
           {
         "No.", "Category", "Brand" , "Product", "Color", "Unit","Description", "Status"
           };

        for (int i = 0; i < headers.Length; i++)
        {
            headerRow.CreateCell(i).SetCellValue(headers[i]);
        }
        var counter = 1;
        // Data rows
        foreach (var product in products)
        {
            var i = counter;
            IRow row = sheet.CreateRow(i);
            row.CreateCell(0).SetCellValue(i);
            row.CreateCell(1).SetCellValue(product.CategoryName);
            row.CreateCell(2).SetCellValue(product.MakeCompany);
            row.CreateCell(3).SetCellValue(product.Name);
            row.CreateCell(4).SetCellValue(product.ColourName);
            row.CreateCell(5).SetCellValue(product.Unit);
            row.CreateCell(6).SetCellValue(product.Description);
            row.CreateCell(7).SetCellValue(product.IsActive ? "Active" : "Inactive");
            counter++;
        }

        // Autosize all columns
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        // Write to memory stream and return as byte array
        using (var exportData = new MemoryStream())
        {
            workbook.Write(exportData);
            return exportData.ToArray();
        }

    }
} 