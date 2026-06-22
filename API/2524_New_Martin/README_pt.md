# Estratégia New Martin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A Estratégia New Martin replica o assessor especialista do MetaTrader "New Martin" executando uma cobertura martingale simétrica em ambos os lados do mercado. A estratégia mantém uma posição comprada e vendida inicial abertas o tempo todo e reequilibra a cobertura quando as médias móveis suavizadas rápida e lenta (SMMA) se cruzam. Quando um lado da cobertura está perdendo, o algoritmo multiplica a exposição nesse lado e simultaneamente realiza ganhos na perna mais lucrativa. As saídas de take profit reciclam a cobertura reabrindo o lado ausente e opcionalmente purgando tanto os melhores quanto os piores desempenhos para manter a grade compacta.

A implementação tem como alvo a API de alto nível do StockSharp e espera um portfólio com suporte a hedge para que as pernas comprada e vendida possam coexistir. As ordens são enviadas como ordens de mercado por simplicidade, refletindo a lógica MQL original onde os preenchimentos são assumidos como imediatos.

## Indicadores e Sinais
- **SMMA rápida (comprimento padrão 5):** rastreia a direção de preço de curto prazo.
- **SMMA lenta (comprimento padrão 20):** representa a tendência dominante.
- **Detecção de cruzamento:** um cruzamento das duas barras completadas anteriores aciona a adição martingale na perna de pior desempenho. O sinal é limitado a uma vez por vela armazenando o horário de abertura da vela do último cruzamento.

## Gestão de Posições
- **Hedge inicial:** assim que os indicadores se formam, a estratégia abre uma posição comprada e uma vendida com o volume inicial configurado. Ambas as negociações usam uma distância de take profit simétrica em pips.
- **Reciclagem de take profit:** quando o preço toca o nível de take profit de qualquer perna, a estratégia fecha essa posição, registra o evento e opcionalmente fecha tanto as posições mais lucrativas quanto as mais perdedoras para realizar ganhos e perdas em pares. Os lados ausentes são imediatamente reabertos com o volume base para que o hedge permaneça equilibrado.
- **Médio martingale:** em cada cruzamento de SMMA, o algoritmo identifica a posição com o menor lucro não realizado. Ele aumenta a exposição naquele lado multiplicando o volume da negociação pelo multiplicador martingale (padrão 1.6) após ajuste ao passo de volume do instrumento. A posição aberta mais lucrativa é fechada logo após a negociação de médio para liberar o lucro bloqueado.

## Gestão de Risco
- **Proteção contra queda de capital:** o maior patrimônio de portfólio observado é rastreado. Se a queda desse pico exceder a porcentagem configurada, todas as posições abertas são liquidadas e a inicialização do hedge é adiada até a próxima vela.
- **Volume base dinâmico:** quando o capital cresce pelo menos pelo multiplicador martingale em relação ao saldo registrado anteriormente, o volume base do hedge é aumentado pelo mesmo multiplicador (respeitando também os limites de volume do exchange). Isso replica o comportamento do EA original onde os lucros são reinvestidos para escalar a grade.
- **Normalização de volume:** cada volume solicitado é arredondado para baixo ao passo de volume do exchange e limitado entre o volume mínimo e máximo do instrumento para evitar rejeições de ordens.

## Parâmetros
- **Take Profit (pips):** distância do preço de entrada para colocar o alvo de take profit para cada perna. Padrão 50 pips.
- **Initial Volume:** volume base por lado do hedge. Padrão 0.1 contratos.
- **Slow MA / Fast MA:** comprimentos dos indicadores SMMA lento e rápido (padrões 20 e 5). O período lento deve permanecer maior que o período rápido.
- **Equity DD %:** queda máxima permitida do pico de capital antes de todas as posições serem fechadas. Padrão 12%.
- **Multiplier:** fator martingale usado para médio abaixo e para escalar o volume base após crescimento de capital importante. Padrão 1.6.
- **Candle Type:** período das velas usadas para cálculos. Padrão velas de 15 minutos, mas pode ser alterado para corresponder ao período do gráfico do EA original.

## Notas
- A estratégia requer contas habilitadas para hedge porque mantém posições compradas e vendidas abertas simultaneamente.
- Ordens de mercado são usadas para entradas e saídas, assim como o especialista MQL que dependia de preenchimentos instantâneos. Adapte a lógica de ordens se controle de slippage for necessário.
- Certifique-se de que os metadados do instrumento (passo de preço, passo de volume, volume mínimo/máximo) estejam corretamente configurados para que a normalização de volume funcione conforme esperado.
