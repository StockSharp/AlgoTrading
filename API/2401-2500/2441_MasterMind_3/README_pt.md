# Estratégia MasterMind 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões extremas usando quatro indicadores **Williams %R** com períodos diferentes. Quando todos os indicadores caem para valores profundos de sobrevenda, a estratégia entra numa posição comprada. Quando todos os indicadores sobem para valores fortes de sobrecompra, entra numa posição vendida.

## Lógica de trading

1. Subscrever velas do período temporal selecionado.
2. Calcular quatro indicadores Williams %R com períodos 26, 27, 29 e 30.
3. **Comprar** quando todos os indicadores estiverem abaixo de `-99.99`.
4. **Vender** quando todos os indicadores estiverem acima de `-0.01`.
5. Os sinais são processados apenas em velas concluídas.

O volume da ordem é retirado da propriedade `Volume` da estratégia. As posições opostas existentes são fechadas automaticamente ao enviar uma ordem de mercado do tamanho necessário.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `WprPeriod1` | Comprimento do primeiro indicador Williams %R | 26 |
| `WprPeriod2` | Comprimento do segundo indicador Williams %R | 27 |
| `WprPeriod3` | Comprimento do terceiro indicador Williams %R | 29 |
| `WprPeriod4` | Comprimento do quarto indicador Williams %R | 30 |
| `CandleType` | Tipo e período temporal das velas | Velas de 1 minuto |

## Notas

* A estratégia usa a API de alto nível com `Bind` para o processamento de indicadores.
* Não inclui níveis de stop loss ou take profit; a posição é invertida em sinais opostos.
