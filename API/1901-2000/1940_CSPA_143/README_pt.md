# Estratégia CSPA 1.43
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação do consultor especialista MQL **CSPA-1_43**. Ela mede a força de um par de moedas usando o Índice de Força Relativa (RSI). Quando o par se torna suficientemente forte ou fraco, a estratégia abre uma posição na direção do momentum predominante e a fecha quando o momentum diminui.

## Lógica

- Assinar velas do ativo selecionado.
- Calcular o valor do RSI para cada vela concluída.
- Abrir uma posição comprada quando o RSI sobe acima do limiar superior.
- Abrir uma posição vendida quando o RSI cai abaixo do limiar inferior.
- Fechar a posição atual quando o RSI retorna à zona neutra.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `StrengthPeriod` | Período usado para o indicador RSI. | `14` |
| `Threshold` | Distância do nível neutro do RSI de 50 usada para gerar sinais. | `10` |
| `CandleType` | Período das velas. | `1 hora` |

## Notas

- A estratégia usa a API de alto nível com vinculação automática de indicadores.
- As ordens são executadas usando ordens a mercado (`BuyMarket` e `SellMarket`).
- Apenas velas concluídas são processadas.
