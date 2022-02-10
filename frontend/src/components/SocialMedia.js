import React from "react";
import facebook from './img/facebook.svg'
import instagram from './img/instagram.svg'
import linkedin from './img/linkedin.svg'
import pinterest from './img/pinterest.svg'
import twitter from './img/twitter.svg'
import youtube from './img/youtube.svg'
import './styles/SocialMedia.scss';

export default function SocialMedia(props) {

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderIcon = (iconName) => {
        switch (iconName.toLowerCase()) {
            case "fafacebook":
            case "facebook":
                return facebook ;
            case "fainstagram":
            case "instagram":
                return instagram ;
            case "fatwitter":
            case "twitter":
                return twitter ;
            case "fayoutube":
            case "youtube":
                return youtube ;
            case "fapinterest":
            case "pinterest":
                return pinterest;
            case "falinkedin":
            case "linkedin":
                return linkedin;
            default:
                return null;
        }
    }

    const renderItems = () => {
        return (
            props.items.map((item, counter) => {
                return (
                    <a href={item.url} key={item.icon} target="_blank"
                        className={`${item.css} social m-0 ${counter === 0 ? '' : 'ml-2 ml-md-3'}`} >
                        <img className="social-icon" src={renderIcon(item.icon)} alt={item.icon} />
                    </a>
                )
            })
        )
    }

    if (props.items == null || props.items.length === 0) return null;

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <div className="social-container">
            {renderItems()}
        </div>
    );
}