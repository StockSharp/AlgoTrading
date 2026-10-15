# JFATL Sistema Digital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia construída em torno da inclinação da média móvel Jurik (JFATL). Abre posições compradas quando a média móvel vira para cima e posições vendidas quando vira para baixo. A ideia imita o sistema digital codificado por cores da versão MQL original.

## Detalhes
- **Critérios de entrada**: A inclinação da média móvel Jurik muda de sinal. Inclinação ascendente abre posição comprada, inclinação descendente abre posição vendida.
- **Comprado/Vendido**: Ambas as direções são operadas.
- **Critérios de saída**: A posição é revertida na inclinação oposta ou fechada pelo gerenciamento de risco.
- **Stops**: Take profit baseado em percentual e stop loss opcional configurado através de `StartProtection`.
- **Valores padrão**: Length = 5, Phase = -100, Timeframe = 4 hours.
- **Filtros**: Nenhum. A estratégia depende exclusivamente da inclinação JMA.
