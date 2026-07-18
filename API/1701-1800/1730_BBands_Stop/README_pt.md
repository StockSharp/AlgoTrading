# Estratégia BBands Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o indicador BBands Stop derivado das Bandas de Bollinger para seguir tendências de mercado. Quando a linha de stop vira para cima, fecha qualquer posição vendida e abre uma comprada. Uma virada para baixo fecha posições compradas e abre vendidas. Os parâmetros controlam o período de Bollinger, o desvio, o offset de risco e as permissões para entrar ou sair de posições compradas e vendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A linha de stop de tendência de alta está ativa.
  - **Vendido**: A linha de stop de tendência de baixa está ativa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal de stop oposto.
- **Stops**: Trailing stop das Bandas de Bollinger.
- **Filtros**: Nenhum.
