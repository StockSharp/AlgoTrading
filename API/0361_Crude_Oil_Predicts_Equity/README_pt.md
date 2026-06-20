# Estratégia de Previsão de Renda Variável com Petróleo Bruto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa a relação entre o petróleo bruto e os retornos de renda variável. Se o retorno do petróleo bruto no último mês for positivo, a estratégia investe em um ETF de renda variável. Caso contrário, rotaciona o capital para um ETF de caixa ou títulos, ficando fora da renda variável quando o petróleo está fraco.

O algoritmo monitora velas diárias e verifica o sinal no primeiro dia de negociação de cada mês. As ordens são submetidas a preços de mercado e respeitam um tamanho mínimo de negociação para evitar execuções pequenas.

## Detalhes

- **Universo**: Um ETF de renda variável, um instrumento de petróleo bruto e um ETF de caixa ou títulos.
- **Sinal**: Ficar comprado no ETF de renda variável quando o retorno do período `Lookback` do petróleo bruto for maior que zero; caso contrário, manter o ETF de caixa.
- **Rebalanceamento**: Mensal, no início do mês.
- **Posicionamento**: Comprado em renda variável ou caixa, nunca em ambos.
- **Parâmetros**:
  - `Equity` – ETF de renda variável alvo.
  - `Oil` – instrumento de petróleo bruto para o sinal.
  - `CashEtf` – ativo defensivo quando o retorno do petróleo é negativo.
  - `Lookback` – número de velas para calcular o retorno do petróleo.
  - `CandleType` – período das velas (padrão: 1 dia).
- **Nota**: O exemplo foca na estrutura e omite custos de transação e slippage.
