import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { initializeAppearance } from './appearance'
import '@fontsource-variable/figtree/wght.css'
import '@fontsource/caprasimo/400.css'
import './styles.css'

initializeAppearance()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
