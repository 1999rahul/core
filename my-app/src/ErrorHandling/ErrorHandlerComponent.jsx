import React from "react";

class ErrorHandlerComponent extends React.Component{
    constructor(props) {
        super(props);
        this.state = {
            error: null,
            hasError:false,
            errorMessage: ''
        }
    }

    // This function is a must to catch error
    static getDerivedStateFromError(error){
        console.log("Error State Updated")
        return {
            error: error,
            hasError: true,
            errorMessage: error.message
        }
    }

    componentDidCatch(errorMessage, error){
        console.log("error" + errorMessage);
        console.log("info"+ JSON.stringify(error));
        // Can call the backend API to log the error and info
    }

    render(){
        if (this.state.hasError){
            return <FallbackComponent errorMessage = {this.state.errorMessage} callback = {() => { this.setState({hasError: false}) }}/>
        
        }
        return this.props.children;
    }
}


function FallbackComponent({errorMessage, callback}){
    return <>
        <div>Some error Occured</div>
        <div>{errorMessage}</div>

        <button onClick={callback}>Try Again</button>
    </>
}




export default ErrorHandlerComponent;
