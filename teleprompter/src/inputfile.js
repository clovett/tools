import React from 'react';
import ReactDOM from 'react-dom';

class InputFile extends React.Component {

    constructor(props, context) {
        super(props, context);

        this.handleChange = this.handleChange.bind(this);
    }

    handleChange(e)
    {
        if (this.props.onChange)
        {
            this.props.onChange(e);
        }
    }

    render() {
        return <input type='file' onChange={ this.handleChange } />;
    }
}


export default InputFile;
