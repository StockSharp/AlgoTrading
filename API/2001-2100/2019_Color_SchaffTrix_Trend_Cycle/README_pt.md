# Estratégia de Ciclo de Tendência Color Schaff TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o oscilador **Schaff Trend Cycle** calculado sobre o MACD baseado em TRIX. O oscilador identifica mudanças cíclicas de tendência e gera sinais de trading quando o ciclo cruza níveis predefinidos.

## Como funciona

1. Dois osciladores TRIX com comprimentos diferentes são calculados para construir uma série MACD.
2. Os valores do MACD são processados por uma dupla transformação estocástica para produzir o Schaff Trend Cycle (STC).
3. Uma posição comprada é aberta quando o STC cruza acima do nível alto e uma posição vendida é aberta quando cruza abaixo do nível baixo.
4. As posições existentes são fechadas quando ocorre um cruzamento oposto.

## Parâmetros

- **Fast TRIX** – comprimento do oscilador TRIX rápido.
- **Slow TRIX** – comprimento do oscilador TRIX lento.
- **Cycle** – período utilizado nos cálculos estocásticos.
- **High Level / Low Level** – limiares superior e inferior para o STC.
- **Stop Loss % / Take Profit %** – parâmetros de controle de risco expressos em porcentagem.
- **Buy/Sell Open/Close** – habilitar ou desabilitar as operações correspondentes.

## Notas

A estratégia usa dados de candles do período selecionado (padrão 4 horas) e executa ordens a mercado. A proteção está habilitada com valores de stop-loss e take-profit. Todo o processamento de indicadores é realizado pela API de alto nível com vínculos automáticos.

Use esta estratégia para fins educacionais e realize backtesting completo antes de operar ao vivo.
