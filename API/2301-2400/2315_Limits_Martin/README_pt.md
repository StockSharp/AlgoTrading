# Estratégia Limits Martin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca ordens limite pareadas acima e abaixo do preço de mercado atual. Cada negociação usa uma distância de passo configurável e dimensionamento de posição martingale opcional para recuperar perdas anteriores.

## Parâmetros
- **Step** – distância em pips entre o preço de mercado e as ordens limite pendentes.
- **Stop Loss** – tamanho do stop protetor em pips para posições abertas.
- **Take Profit** – tamanho do lucro alvo em pips para posições abertas.
- **Use Martingale** – habilita o aumento de volume após uma negociação perdedora.
- **Loss Limit** – número máximo de negociações perdedoras consecutivas antes de redefinir o volume.
- **Volume** – volume inicial da ordem.
- **Use MegaLot** – dobra o volume em vez de adicionar o volume base quando o martingale está ativo.
- **Candle Type** – tipo de dados de vela usado para processamento.

## Lógica de negociação
1. Quando não há posição aberta ou ordem ativa, a estratégia coloca uma ordem Buy Limit abaixo do último fechamento e uma ordem Sell Limit acima, ambas à distância `Step` especificada.
2. Após a execução de uma ordem, a ordem pendente oposta permanece, permitindo apenas uma posição ativa por vez.
3. A posição é fechada quando o nível de stop loss ou take profit é atingido.
4. Após uma negociação perdedora, o volume da posição pode ser aumentado de acordo com as configurações do martingale.

## Notas
- A estratégia usa a API de alto nível do StockSharp com a abordagem `Bind` para o tratamento de dados de velas.
- Todos os comentários dentro do código são escritos em inglês para atender às convenções do repositório.
