# Estratégia de Formas do Corpo das Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera com base em onde uma vela abre e fecha dentro de seu intervalo.
Entra comprado quando a vela abre próxima à sua mínima e fecha próxima à sua máxima, demonstrando forte pressão altista.
Entra vendido quando a vela abre próxima à sua máxima e fecha próxima à sua mínima, indicando forte pressão baixista.

A abordagem baseia-se puramente na ação do preço e pode ser aplicada a qualquer mercado líquido.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Open near Low && Close near High`
  - Vendido: `Open near High && Close near Low`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `BodyThreshold` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Padrão de candlestick
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
