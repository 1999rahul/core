import UserDemo from "./Routing/Questions/UserProfile/User";
import ErrorHandlerComponent from "./ErrorHandling/ErrorHandlerComponent";

function App() {
  return <ErrorHandlerComponent>
    <UserDemo />
  </ErrorHandlerComponent>

}

export default App
