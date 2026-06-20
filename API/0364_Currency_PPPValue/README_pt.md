# Estratégia de Valor PPP de Moedas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Valor PPP de Moedas procura distorções de preço em relação à paridade do poder de compra (PPP). Moedas negociadas abaixo do seu valor PPP são compradas, enquanto as que negociam acima são vendidas a descoberto. A carteira é rebalanceada mensalmente para manter a exposição comprado/vendido.

Como os dados de PPP são atualizados com pouca frequência, as negociações são realizadas apenas quando o ajuste necessário supera um valor mínimo em USD. O código de exemplo fornece a estrutura para classificar as moedas, mas deixa o cálculo real do PPP como marcador de posição.

## Detalhes

- **Universo**: Conjunto de pares de moedas com estimativas de PPP disponíveis.
- **Sinal**: Comprado nas `K` moedas mais subavaliadas e vendido nas `K` mais sobrevalorizadas.
- **Rebalanceamento**: Mensal.
- **Posicionamento**: Comprado/Vendido, igual ponderação.
- **Parâmetros**:
  - `Universe` – moedas negociáveis.
  - `K` – número de moedas para comprado e vendido.
  - `MinTradeUsd` – tamanho mínimo de negociação em USD.
  - `CandleType` – período das velas (padrão: 1 dia).
- **Nota**: A obtenção do desvio PPP (`TryGetPPPDeviation`) não está implementada e deve ser fornecida pelo usuário.
