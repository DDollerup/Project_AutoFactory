using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Example.Models; // Get reference to our Models!
using Example.Models.ViewModels; // Get reference to our ViewModels!
using Example.Factories; // Where our DBContext relies

namespace Example
{
    class Program
    {

        /*
         * Set your connectionstring in your web.config file, and name it "String"
         * You can edit the name of the connectionString in the constructor of AutoFactory.cs
         */

        private static readonly DBContext context = new DBContext();
        static void Main(string[] args)
        {
            /*
                This is example only, requires a database and connectionstring in order to work.
            */

            #region Get Examples
            // Get by ID
            Product getbyID = context.ProductFactory.Get(1);

            // Get by field and value
            Product getByValue = context.ProductFactory.Get("Token", "tokenexamplekey123123123123");

            // Get all
            List<Product> products = context.ProductFactory.GetAll();

            // Get all by field and value
            List<Product> productsFiltered = context.ProductFactory.GetAllBy("Bar", "Foo");

            /* Let's try some GET examples with a ViewModel  */
            // We get our product from the database!
            Product product = context.ProductFactory.Get(1);

            // And we create a VM from that product.
            // Here we can use GetAllByJoin to get all the categories, this product belongs to.
            // It does this by checking a many to many tables, with reference to TokenKey (Which is in the many to many relation table) and the products token.
            ProductVM productVM = new ProductVM()
            {
                Product = product,
                Categories = context.CategoryFactory.GetAllByJoin<CategoryRelation>("TokenKey", product.Token), // Get Categories from a many to many relationship
                Subcategories = context.SubCategoryFactory.GetAllByJoin<SubCategoryRelation>("TokenKey", product.Token), // Get Subcategories from a many to many relationship
                Images = context.ImageFactory.GetAllByJoin<Image>("TokenKey", product.Token) // Get images from a many to many relationship
            };

            // We can also search a specific table
            List<Product> productsSearchResults = context.ProductFactory.SearchBy("foo", "Title", "Description");

            // We can also search for products with specific one to many relations in different tables
            // This could be if we searched for 'categoryFoo' and we want all the products that has that categoryID
            List<Product> productsSearchResultsOneToMany = context.ProductFactory.SearchByJoin<Category>("categoryFoo", "Title", "Description");
            // And we can do this with multiple one to many relations
            List<Product> productsSearchResultsMultipleOneToMany = context.ProductFactory.SearchByJoin<Category, SubCategory>("categoryFoo", "Title", "Description");
            #endregion

            #region Insert Examples
            // So, we want to insert a product, so we create a dummy product.
            Product dummy = new Product()
            {
                Title = "Dummy",
                Description = "Dummy Description",
                CategoryID = 1,
                SubCategoryID = 3
            };

            // Now, Product contains both a Token value and an ID.
            // While Token is optional (Just don't add it to your model or table), ID is required as the first column in your table.
            dummy = context.ProductFactory.Insert(dummy);
            // After the insert statement, dummy now contains it's ID and Token, which we can now play with. (Also optional, you can also just insert it)

            // We could for example, add many to many relations based on a number of category ids and subcategory ids
            // by using the dummy.
            List<int> categories = new List<int>(new int[] { 1, 2, 3, 4 });
            List<int> subcategories = new List<int>(new int[] { 1, 2, 3, 4 });

            categories.ForEach(x => context.CategoryRelationFactory.Insert(new CategoryRelation() { CategoryID = x, TokenKey = dummy.Token }));
            subcategories.ForEach(x => context.SubCategoryRelationFactory.Insert(new SubCategoryRelation() { SubCategoryID = x, TokenKey = dummy.Token }));

            // Extra
            // We could also check if there's a product with the same name, before we insert
            if (!context.ProductFactory.ExistsBy("Title", dummy.Title))
            {
                // Did not exist, so we add it
                context.ProductFactory.Insert(dummy);
            }
            #endregion

            #region Update Examples

            // So, we want to update our table, and we will use our dummy reference. (It contains the ID, which is required to update the product)

            // We change the properties.
            dummy.CategoryID = 5;
            dummy.Title = "Foo";
            dummy.Description = "Bar";

            // And we update it, simple as that.
            context.ProductFactory.Update(dummy);
            #endregion

            #region Delete Examples

            // We, of course, need to be able to delete something, we can do this 2 ways.

            // We can delete an entity by its ID
            context.ProductFactory.Delete(1);
            // Or we can delete multiple elements, by a field and value. So this deletes all products that has categoryid = 5.
            context.ProductFactory.DeleteBy("CategoryID", 5);
            // And that's it.
            #endregion

            #region Extra Usage Examples

            // There are different 'Easy of Life' methods included.

            // We can count everything
            int countAll = context.ProductFactory.Count();

            // We can count by field and value, so this counts all products with categoryid = 5
            int countByValue = context.ProductFactory.CountBy("CategoryID", 5);

            // We can get the latest entity, this by default, gets the latest entity by ID, but can be changes with the parameters.
            Product latestProduct = context.ProductFactory.GetLatest();

            // We can get random entities
            List<Product> randomProducts = context.ProductFactory.TakeRandom(5);
            #endregion
        }
    }
}