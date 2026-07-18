# Estratégia ValidateMe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia ValidateMe transporta a estrutura de validação básica do consultor especialista MQL4 original. A lógica centra-se na verificação da disponibilidade de fundos, verificando se as distâncias stop-loss e take-profit respeitam as restrições cambiais e, em seguida, disparando uma ordem de mercado única na direção escolhida. A estratégia monitoriza continuamente os eventos de execução de negociações e abre uma nova posição apenas quando não existem posições ou ordens ativas.

## Lógica de negociação

1. A estratégia assina os dados de tick da segurança configurada.
2. Quando a estratégia está online, formada e a negociação é permitida, ela verifica se nenhuma posição aberta e nenhuma ordem ativa está presente.
3. Em seguida, envia uma ordem de mercado na direção configurada (compra ou venda) usando o tamanho de lote definido.
4. Um módulo de proteção anexa imediatamente ordens de take-profit e stop-loss calculadas a partir de distâncias de pip, garantindo a conformidade com os níveis de stop da corretora (ajustados para preços fracionários).
5. Assim que a posição for fechada, a estratégia aguarda o próximo tick e repete a validação antes de enviar uma nova ordem.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| **Take Profit (pips)** | Distância do preço de entrada ao take-profit em pips. Deve ser maior que zero. |
| **Stop Loss (pips)** | Distância do preço de entrada ao stop loss em pips. Deve ser maior que zero. |
| **Muitos** | Volume de negociação em lotes utilizados para cada ordem de mercado. |
| **Direção** | Direção da ordem de mercado (Compra ou Venda). |

## Gestão de risco

* A estratégia usa `StartProtection` com compensações absolutas para registrar ordens de take-profit e stop-loss.
* O tamanho do pip é calculado a partir da etapa do preço do título e da precisão decimal para imitar o comportamento MetaTrader (símbolos de 5 e 3 dígitos usam um tamanho de ponto dez vezes maior).
* A estratégia dispara novos pedidos somente se nenhum pedido existente estiver ativo, evitando o empilhamento de pedidos.

## Notas de uso

* Anexe a estratégia a um título e defina o volume e a direção desejados.
* Configure as distâncias de take-profit e stop-loss em pips de acordo com os requisitos da corretora.
* A estratégia não se baseia em indicadores e pretende ser um quadro de validação e não um sistema comercial completo.
* O controle de risco do portfólio (por exemplo, redução máxima) pode ser combinado externamente, se necessário.
