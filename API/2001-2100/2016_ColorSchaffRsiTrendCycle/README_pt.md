# Estratégia de Ciclo de Tendência Color Schaff RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema seguidor de tendência baseado no oscilador Color Schaff RSI Trend Cycle (STC). A estratégia reage às transições de cor do indicador STC para entrar e sair de posições compradas e vendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Cor do indicador duas barras atrás > 5 e última barra < 6.
  - **Vendido**: Cor do indicador duas barras atrás < 2 e última barra > 1.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Posições compradas fecham quando a cor do indicador duas barras atrás < 2.
  - Posições vendidas fecham quando a cor do indicador duas barras atrás > 5.
- **Indicadores**: Color Schaff RSI Trend Cycle.
- **Valores padrão**:
  - `Fast RSI` = 23
  - `Slow RSI` = 50
  - `Cycle` = 10
  - `High Level` = 60
  - `Low Level` = -60
- **Período**: Velas de 4 horas por padrão.
- **Stops**: Nenhum.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
