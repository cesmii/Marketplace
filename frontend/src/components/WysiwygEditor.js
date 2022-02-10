import { useState, useEffect } from 'react';
import { Editor } from 'react-draft-wysiwyg';
import { ContentState, convertToRaw, EditorState } from 'draft-js';
import { convertToHTML } from 'draft-convert';
import { stateToHTML } from 'draft-js-export-html';
import htmlToDraft from 'html-to-draftjs';
import 'react-draft-wysiwyg/dist/react-draft-wysiwyg.css';

import { generateLogMessageString } from '../utils/UtilityService';
import './styles/WysiwygEditor.scss';

const CLASS_NAME = "WysiwygEditor";

function WysiwygEditor(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const initContent = (val) => {
        console.log(generateLogMessageString('initContent', CLASS_NAME));

        val = val.replace(/<figure>/g, "").replace(/<\/figure>/g, "");

        const blocksFromHtml = htmlToDraft(val);
        const { contentBlocks, entityMap } = blocksFromHtml;
        const contentState = ContentState.createFromBlockArray(contentBlocks, entityMap);
        return EditorState.createWithContent(contentState);
    };

    //const [_value, setValue] = useState(EditorState.createEmpty());
    //const [_value, setValue] = useState(props.value == null || props.value === '' ? EditorState.createEmpty() : props.value);
    //const [_value, setValue] = useState(EditorState.createWithContent(convertFromHTML(props.value)));
    const [_value, setValue] = useState(initContent(props.value));

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------
    useEffect(() => {
        //setValue(EditorState.createWithContent(convertFromHTML(props.value)));

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Cleanup', CLASS_NAME));
        };
    }, [props.value]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const onChange = (state) => {
        console.log(generateLogMessageString('onChange', CLASS_NAME));
        setValue(state);

        if (props.onChange) {
            var valSimpleHtml = convertToHTML(_value.getCurrentContent());
            var valRaw = convertToRaw(_value.getCurrentContent());
            var valHtml = stateToHTML(state.getCurrentContent());
            console.log(generateLogMessageString(`onChange||value:${valSimpleHtml}`, CLASS_NAME));
            console.log(generateLogMessageString(`onChange||raw:${JSON.stringify(valRaw)}`, CLASS_NAME));
            console.log(generateLogMessageString(`onChange||stateToHTML:${valHtml}`, CLASS_NAME));
            var item = {
                target: { id: props.id, value: valHtml === '<p></p>' ? '' : valHtml }
            };
            //update state in parent
            props.onChange(item);
            //trigger validation in parent
            //if (props.onValidate) props.onValidate(item);
        }
    };

    const uploadCallback = (file) => {
        console.log(generateLogMessageString('uploadCallback', CLASS_NAME));

        //this will upload locally and assign the img src as base64 string. 
        return new Promise(
            (resolve, reject) => {
                if (file) {
                    let reader = new FileReader();
                    reader.onload = (e) => {
                        resolve({ data: { link: e.target.result } })
                    };
                    reader.readAsDataURL(file);
                }
            }
        );
    };

    /*
    const onBlur = (event, editorState) => {
        console.log(generateLogMessageString('onBlur', CLASS_NAME));
        var item = {
            target: { id: props.id, value: props.value }
        };
        //trigger validation in parent
        if (props.onValidate) props.onValidate(item);
    };
    */

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    const _config = {
        image: {
            uploadCallback: uploadCallback, uploadEnabled: true,
            previewImage: true,
            defaultSize: {
                height: 'auto',
                width: '100%',
            },
            alt: { present: true, mandatory: false }
        }
    };

    return (
        <>
        <Editor
            toolbar={_config}
            editorState={_value}
            onEditorStateChange={onChange}
            editorClassName={`wysiwyg-editor ${props.className}`}
            toolbarClassName='wysiwyg-toolbar'
        />
            {/* preview of rendered output
            <div className="col-md-12">
                <div className="mb-0" dangerouslySetInnerHTML={{ __html: props.value }} ></div>
            </div>
             */}
        </>
    );
}


export { WysiwygEditor };
