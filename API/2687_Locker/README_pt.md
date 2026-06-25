# Locker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cobertura baseada em grade que alterna ordens de mercado compradas e vendidas para bloquear perdas flutuantes e capturar uma pequena porcentagem de lucro sobre o saldo da conta.

## Lógica de trading
* Abre a primeira posição comprada com o volume inicial configurado assim que o primeiro candle fecha.
* Rastreia cada entrada subsequente e mantém um livro interno de pernas de compra e venda para estimar o lucro combinado não realizado e realizado.
* Se o número de pernas ativas chegar a oito, a estratégia fecha o par de compra/venda disponível mais antigo para manter a exposição sob controle antes de fazer qualquer outra coisa naquele candle.
* Quando o lucro combinado sobe acima da porcentagem alvo do valor do portfólio, fecha todas as posições restantes e redefine o estado interno.
* Quando o lucro combinado cai abaixo do alvo negativo, mede a distância entre o último preço de entrada e o preço de mercado atual. Se o preço subiu pelo passo configurado, adiciona uma nova perna vendida; se o preço caiu a mesma distância, adiciona uma nova perna comprada.
* Cada fechamento usa ordens de mercado na direção oposta à entrada registrada para que a cobertura seja neutralizada imediatamente.

## Parâmetros
* **Profit %** – porcentagem do valor atual do portfólio que deve ser bloqueada antes de achatar o livro.
* **Start Volume** – quantidade usada para a primeira entrada comprada que inicia a grade.
* **Step Volume** – quantidade enviada para cada ordem de cobertura depois que o limite de perda é ultrapassado.
* **Step Points** – número de passos de preço entre níveis de grade; multiplicado pelo passo de preço do instrumento para calcular a distância de preço real.
* **Enable Automation** – interruptor mestre que pausa toda a lógica de trading quando desativado.
* **Candle Type** – série de candles usada para acionar a lógica de decisão em cada barra concluída.

A conversão replica a lógica do expert original do MetaTrader, adaptando o posicionamento de ordens à API de alto nível do StockSharp e armazenando o estado detalhado da negociação dentro da estratégia para que o cálculo de lucros corresponda à versão MQL.
