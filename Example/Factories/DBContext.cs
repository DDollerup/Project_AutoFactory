using Example.Models;

namespace Example.Factories
{
    /// <summary>
    /// On-Demand loading of different factories
    /// </summary>
    public class DBContext
    {
        private static volatile DBContext INSTANCE;
        public static DBContext Instance
        {
            get
            {
                if (INSTANCE == null)
                {
                    INSTANCE = new DBContext();
                }
                return INSTANCE;
            }
        }
        
        private AutoFactory<PageIndex> pageIndexFactory;
        private AutoFactory<Product> productFactory;
        private AutoFactory<Category> categoryFactory;
        private AutoFactory<SubCategory> subCategoryFactory;
        private AutoFactory<Image> imageFactory;
        private AutoFactory<CategoryRelation> categoryRelationFactory;
        private AutoFactory<SubCategoryRelation> subCategoryRelationFactory;

        public AutoFactory<PageIndex> PageIndexFactory
        {
            get
            {
                if (pageIndexFactory == null)
                {
                    pageIndexFactory = new AutoFactory<PageIndex>();
                }
                return pageIndexFactory;
            }
        }

        public AutoFactory<Product> ProductFactory
        {
            get
            {
                if (productFactory == null)
                {
                    productFactory = new AutoFactory<Product>();
                }
                return productFactory;
            }
        }

        public AutoFactory<Category> CategoryFactory
        {
            get
            {
                if (categoryFactory == null)
                {
                    categoryFactory = new AutoFactory<Category>();
                }
                return categoryFactory;
            }
        }

        public AutoFactory<SubCategory> SubCategoryFactory
        {
            get
            {
                if (subCategoryFactory == null)
                {
                    subCategoryFactory = new AutoFactory<SubCategory>();
                }
                return subCategoryFactory;
            }
        }

        public AutoFactory<Image> ImageFactory
        {
            get
            {
                if (imageFactory == null)
                {
                    imageFactory = new AutoFactory<Image>();
                }
                return imageFactory;
            }
        }

        public AutoFactory<CategoryRelation> CategoryRelationFactory
        {
            get
            {
                if (categoryRelationFactory == null)
                {
                    categoryRelationFactory = new AutoFactory<CategoryRelation>();
                }
                return categoryRelationFactory;
            }
        }

        public AutoFactory<SubCategoryRelation> SubCategoryRelationFactory
        {
            get
            {
                if (subCategoryRelationFactory == null)
                {
                    subCategoryRelationFactory = new AutoFactory<SubCategoryRelation>();
                }
                return subCategoryRelationFactory;
            }
        }
    } 
}
