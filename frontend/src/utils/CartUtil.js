//import { AppSettings } from "../../utils/appsettings";
//import { generateLogMessageString } from "../../utils/UtilityService";

//const CLASS_NAME = "CartUtil";

//-------------------------------------------------------------------
// add to cart
//-------------------------------------------------------------------
export function updateCart(cart, item, quantity) {

    if (cart == null) cart = {};
    if (cart.items == null) cart.items = [];

    //loop through cart and see if item is there. If not, add, if there, add new quantity to existing.
    let cartItem = cart.items.find(x => { return x.marketplaceItem?.id === item.id; });
    if (cartItem == null) {
        cartItem = { marketplaceItem: item, quantity: quantity };
        cart.items.push(cartItem);
    }
    else {
        cartItem.quantity += quantity;
    }
    return cart;
}

