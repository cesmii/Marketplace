import { convertHtmlToString, getImageUrl } from "./UtilityService";

//const CLASS_NAME = "schemaOrgUtil";

///--------------------------------------------------------------------------
/// https://schema.org/
/// This is a collaborative effort to allow search engines to display rich text snippets and search results for pages
/// The content can either be tagged with special attributes within the HTML or it can be denoted in a special script tag.
///     Or it can be a mixture. 
/// We are adopting the script tag approach due to increased flexibility to get to the data we need w/o having to adjust HTML to 
///     fit into the structure.
///--------------------------------------------------------------------------
export function renderSchemaOrgContentMarketplaceItem(item) {
    if (item == null || item.id == null) return;

    const type = item.type?.name.toLowerCase() === 'sm hardware' ? 'Product'
        : item.type?.name.toLowerCase() === 'sm profile' ? 'Code' : 'SoftwareApplication';
    const imageUrl = item.imageLandscape != null ? getImageUrl(item.imageLandscape) : getImageUrl(item.imagePortrait);

    //build  up category list into a unified comma separated string
    let cats = generateCatsList(item);

    //manually add type as category
    if (item.type?.name != null) cats.push(item.type.name);

    //manually add categories for sm profile
    if (item.type?.name.toLowerCase() === 'sm profile') {
        cats.push('OPC UA Nodeset');
    }

    let result = {
        '@context': 'https://schema.org'
        , '@type': type
        , 'name': item.displayName != null ? item.displayName : item.name
        , 'url': item.namespace != null ? item.namespace : window.location.href
        , 'description': convertHtmlToString(item.abstract)
        , 'version': item.version
        , 'image': imageUrl
    };
    if (cats.length > 0) {
        result.applicationCategory = cats.join(', ');
    }
    if (item.publisher != null) {
        result.publisher = { '@type': 'Organization', 'name': item.publisher.displayName };
    }

    return (
        <script type="application/ld+json" nonce="">
            {JSON.stringify(result)}
        </script>
    );

}

export function renderSchemaOrgContentPublisher(item) {
    if (item == null || item.id == null) return;

    //build  up category list into a unified comma separated string
    let cats = generateCatsList(item);

    let description = convertHtmlToString(item.description).trimStart().trimEnd();
    description = description.length > 300 ? description.substring(0, 300) + '...' : description;

    let result = {
        '@context': 'https://schema.org'
        , '@type': 'Organization'
        , 'name': item.displayName != null ? item.displayName : item.name
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
