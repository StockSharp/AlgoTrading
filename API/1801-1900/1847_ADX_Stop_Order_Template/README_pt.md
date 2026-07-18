# Estratégia de Modelo de Ordem Stop ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia demonstra como colocar ordens stop pendentes usando o Average Directional Index (ADX) e seus componentes de Movimento Direcional. Recria a lógica central de um modelo MQL clássico: quando o mercado mostra uma tendência forte e as linhas +DI e -DI se cruzam, o sistema coloca uma ordem de compra stop ou venda stop a uma distância fixa. Níveis de stop-loss e take-profit de proteção são gerenciados automaticamente.

O exemplo é intencionalmente simples e focado no tratamento de ordens. Os traders podem estendê-lo com filtros adicionais ou regras de gestão de capital para construir sistemas mais avançados.

## Detalhes

- **Critérios de entrada**:
  - Valor ADX acima do parâmetro `ADX Threshold`.
  - **Comprado**: `+DI` maior que `-DI` e duas velas atrás `+DI` estava abaixo de `-DI`.
  - **Vendido**: `+DI` menor que `-DI` e duas velas atrás `+DI` estava acima de `-DI`.
  - O spread atual deve estar abaixo do parâmetro `Max Spread`.
- **Colocação de ordens**:
  - Ordens stop pendentes são colocadas `Pips` passos de preço afastados do bid ou ask atual.
  - Apenas uma ordem pendente está ativa por vez; ordens antigas são canceladas quando um novo sinal aparece.
- **Critérios de saída**:
  - Posições compradas são fechadas quando `-DI` sobe acima de `+DI`.
  - Posições vendidas são fechadas quando `+DI` sobe acima de `-DI`.
- **Stops**:
  - Stop-loss e take-profit são aplicados via `StartProtection` usando os parâmetros `Stop Loss` e `Take Profit`.
- **Valores padrão**:
  - `ADX Period` = 14
  - `ADX Threshold` = 5
  - `Pips` = 10 passos de preço
  - `Take Profit` = 1000 passos de preço
  - `Stop Loss` = 500 passos de preço
  - `Max Spread` = 20 passos de preço
  - `Candle Type` = velas de 15 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ADX, DMI
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Filtro de spread: Sim
