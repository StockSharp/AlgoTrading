# Estratégia de Zonas SMC de Blocos de Ordens
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia identifica máximas e mínimas de swing para definir zonas premium e de desconto. Uma média móvel simples atua como filtro de tendência e blocos de ordens recentes confirmam as entradas. As operações são executadas quando o preço se move de uma zona em direção ao equilíbrio com confirmação do bloco de ordens, usando um stop loss percentual para proteção.

## Detalhes

- **Critérios de entrada**:
  - Fechamento abaixo do equilíbrio mas acima da zona de desconto e SMA para operações compradas.
  - Fechamento acima do equilíbrio mas abaixo da zona premium e SMA para operações vendidas.
  - O preço deve tocar o nível do bloco de ordens respectivo.
- **Comprado/Vendido**: Configurável comprado, vendido ou ambos.
- **Critérios de saída**: Sinal oposto ou stop loss.
- **Stops**: Stop loss percentual.
- **Valores padrão**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Tendência e SMC
  - Direção: Definido pelo usuário
  - Indicadores: SMA, Highest, Lowest
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
