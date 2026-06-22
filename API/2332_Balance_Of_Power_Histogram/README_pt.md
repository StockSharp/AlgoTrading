# Estratégia de Histograma de Balance Of Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação do expert original do MetaTrader de `MQL/16214`. Utiliza o indicador **Balance of Power** (BOP) para detectar mudanças de momentum no mercado.

## Lógica

1. A estratégia calcula o Balance of Power para cada vela concluída:
   
   $$BOP = \frac{Close - Open}{High - Low}$$
2. Três valores consecutivos de BOP são comparados.
   - Quando o valor anterior é menor que o anterior a ele e o valor atual é maior que o anterior, o BOP vira para cima e a estratégia entra em uma posição comprada.
   - Quando o valor anterior é maior que o anterior a ele e o valor atual é menor que o anterior, o BOP vira para baixo e a estratégia entra em uma posição vendida.
3. A posição é alterada apenas após uma vela concluída para evitar sinais falsos.

## Parâmetros

- **CandleType** – período das velas usadas para cálculos. O padrão são velas de quatro horas.

## Notas

Este port foca no comportamento central da estratégia original e não implementa as opções avançadas de gestão de capital da versão MQL.
