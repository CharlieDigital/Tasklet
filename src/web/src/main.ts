import "virtual:uno.css";
import "./style.css";
import router from "./router";
import App from "./App.vue";
import { apiBaseUrl } from "./runtime-env";

// https://www.naiveui.com/en-US/light/docs/fonts
// import "vfonts/Lato.css";
// import "vfonts/FiraCode.css";

const pinia = createPinia();
const app = createApp(App);

console.debug("API Base URL:", apiBaseUrl);

app.use(pinia);
app.use(router);

app.mount("#app");
