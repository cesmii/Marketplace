import { getImageUrl } from "./UtilityService";

//const CLASS_NAME = "schemaOrgUtil";

///--------------------------------------------------------------------------
/// https://schema.org/
/// This is a collaborative effort to allow search engines to display rich text snippets and search results for pages
/// The content can either be tagged with special attributes within the HTML or it can be denoted in a special script tag.
///     Or it can be a mixture. 
/// We are adopting the script tag approach due to increased flexibility to get to the data we need w/o having to adjust HTML to 
///     fit into the structure.
///--------------------------------------------------------------------------
export function renderSchemaOrgContentHome(title, description) {

    const logo = `${window.location.origin}/CESMII-Icon-192x192.png`;

    let result = {
        '@context': 'https://schema.org'
        , '@type': 'WebSite'
        , 'name': title
        , 'description': description
        , 'image': logo
        , 'publisher': { '@type': 'Organization', 'name': 'CESMII', 'logo': logo, 'url': 'https://www.cesmii.org/'}
    };
    return (
        <script type="application/ld+json" nonce="">
            {JSON.stringify(result)}
        </script>
    );
}

export function renderSchemaOrgContentMarketplaceItemList(title, description) {

    const logo = `${window.location.origin}/CESMII-Icon-192x192.png`;

    //note we leave out the item list contents because we only show first X items and thus the items list will not
    //reflect what is entirely in the library and we don't want the entire library to be pulled down 
    let result = {
        '@context': 'https://schema.org'
        , '@type': 'ItemList'
        , 'name': title
        , 'description': description
        , 'image': logo
    };
    return (
        <script type="application/ld+json" nonce="">
            {JSON.stringify(result)}
        </script>
    );
}

export function renderSchemaOrgContentMarketplaceItem(title, description, item) {
    if (item == null || item.id == null) return;

    const type = item.type?.name.toLowerCase() === 'sm hardware' ? 'Product'
        : item.type?.name.toLowerCase() === 'sm profile' ? 'Code' : 'SoftwareApplication';
    const imageUrl = item.imageLandscape != null ? getImageUrl(item.imageLandscape) : getImageUrl(item.imagePortrait);

    //build  up category list into a unified comma separated string
    let cats = generateCatsList(item);
    //manually add type as category for non-profiles
    if (item.type?.name != null) cats.push(item.type.name);

    //manually add keywords for sm profile
    if (item.type?.name.toLowerCase() === 'sm profile') cats.push('OPC UA Nodeset');

    let result = {
        '@context': 'https://schema.org'
        , '@type': type
        , 'name': title
        , 'url': item.namespace != null ? item.namespace : window.location.href
        , 'description': description
        , 'image': imageUrl
    };

    //type specific treatment
    switch (item.type?.name.toLowerCase()) {
        case "sm hardware":
            if (cats.length > 0) {
                result.keywords = cats.join(', ');
            }
            if (item.publisher != null) {
                result.manufacturer = { '@type': 'Organization', 'name': item.publisher.displayName };
            }
            break;
        case "sm profile":
            if (cats.length > 0) {
                result.keywords = cats.join(', ');
            }
            if (item.publisher != null) {
                result.publisher = { '@type': 'Organization', 'name': item.publisher.displayName };
            }
            result.version = item.version;
            break;
        case "sm app":
        default:
            if (cats.length > 0) {
                result.applicationCategory = cats.join(', ');
            }
            if (item.publisher != null) {
                result.publisher = { '@type': 'Organization', 'name': item.publisher.displayName };
            }
            result.version = item.version;
            break;
    }

    return (
        <script type="application/ld+json" nonce="">
            {JSON.stringify(result)}
        </script>
    );

}

export function renderSchemaOrgContentPublisher(title, description, item) {
    if (item == null || item.id == null) return;

    //build  up category list into a unified comma separated string
    let cats = generateCatsList(item);

    let result = {
        '@context': 'https://schema.org'
        , '@type': 'Organization'
        , 'name': title
        , 'description': description
    };
    if (cats.length > 0) {
        result.keywords = cats.join(', ');
    }
    if (item.companyUrl != null) {
        result.url = item.companyUrl;
    }

    return (
        <script type="application/ld+json" nonce="">
            {JSON.stringify(result)}
        </script>
    );

}

const generateCatsList = (item) => {
    let result = item.categories == null ? [] : item.categories.map(c => c.name);
    let verts = item.industryVerticals == null ? [] : item.industryVerticals.map(c => c.name);
    result = result.concat(verts);
    if (item.metatags != null) result = result.concat(item.metatags);
    return result;
}
