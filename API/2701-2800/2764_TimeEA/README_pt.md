# Estratégia TimeEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia TimeEA** replica o consultor especialista original do MetaTrader "TimeEA" dentro do StockSharp. Ela gerencia uma única posição baseando-se exclusivamente na hora do dia: abre em um momento configurado, mantém a posição em uma direção fixa e sai ou em um horário de fechamento programado ou quando os níveis opcionais de stop-loss/take-profit são violados.

Ao contrário dos sistemas baseados em indicadores, esta implementação foca no gerenciamento disciplinado de sessão. Garante apenas uma entrada por dia de trading, limpa a exposição oposta antes de abrir e aplica distâncias mínimas configuráveis para ordens protetoras que imitam as limitações de nível de stop do corretor.

## Como funciona

1. A estratégia assina uma série de velas configurável (1 minuto por padrão) e avalia apenas as velas concluídas.
2. Quando o fechamento de uma vela cruza o **Horário de abertura** configurado, a estratégia:
   - Fecha qualquer posição oposta que ainda possa estar aberta.
   - Coloca uma ordem a mercado na direção escolhida (Compra ou Venda) com o volume especificado.
   - Registra preços de stop-loss e take-profit em pontos (passos de preço) a partir da entrada, aplicando o multiplicador de distância mínima.
3. Durante a sessão, a estratégia monitora as velas:
   - Se uma vela toca o nível de stop-loss ou take-profit armazenado, a posição é fechada imediatamente.
   - Se a vela cruzar a janela do **Horário de fechamento**, a posição é nivelada independentemente de lucro ou perda.
4. Após fechar a negociação (por stop, alvo ou agenda), a estratégia permanece sem posição até o próximo dia de trading.

Este fluxo reproduz o comportamento "abrir uma vez por dia" da versão do MetaTrader que dependia de comparações `TimeCurrent()` e `Time[0]`.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| **Open Time** | Hora do dia para abrir a negociação. Aceita `HH:MM:SS`. |
| **Close Time** | Hora do dia para nivelar todas as posições. Pode ser no mesmo dia ou passar para o dia seguinte. |
| **Position Type** | Direção da posição (`Buy` ou `Sell`). |
| **Order Volume** | Quantidade usada ao enviar a ordem a mercado. |
| **Stop Loss (points)** | Distância em passos de preço para o stop protetor. Definir como 0 para desativar. |
| **Take Profit (points)** | Distância em passos de preço para o alvo de lucro. Definir como 0 para desativar. |
| **Minimum Distance Multiplier** | Offset mínimo aplicado tanto ao stop quanto ao alvo (em passos de preço) para emular a verificação original do nível de stop contra o spread. |
| **Candle Type** | Série de dados usada para detectar limites de tempo. O padrão são velas de 1 minuto. |

## Notas práticas

- **Entrada única por dia** – Uma vez que o horário de abertura é acionado, a estratégia não reingressará até o próximo dia de calendário mesmo que a posição tenha sido parada antecipadamente.
- **Suporte a cruzamento de meia-noite** – Tanto os horários de abertura quanto de fechamento podem ser definidos antes ou depois da meia-noite. O auxiliar respeita sessões que continuam após 00:00.
- **Gerenciamento de volume** – As ordens a mercado respeitam o parâmetro `Order Volume`; ajustar para o tamanho do contrato do instrumento selecionado.
- **Emulação de nível de stop** – O multiplicador de distância mínima garante que stops/alvos fiquem pelo menos um número definido de pontos do ponto de entrada, refletindo a regra original "spread × multiplicador".
- **Requisitos de dados** – A estratégia depende de velas consistentes para o timing. Usar intervalos de tempo locais da bolsa para evitar a deriva de fuso horário.
- **Gestão de risco** – Stops e alvos são mantidos internamente; nenhuma ordem OCO do lado do servidor é criada. Quando uma vela cruzar os limiares, a estratégia emite uma ordem a mercado para sair.

## Casos de uso

- Automatizar entradas baseadas em sessão (p. ex., abrir posições na abertura de Londres ou Nova York).
- Executar estratégias de viés direcional onde a direção é conhecida de antemão, mas a execução deve seguir um calendário preciso.
- Emular gatilhos de tempo no estilo MetaTrader dentro da API de alto nível do StockSharp sem temporizadores manuais.

## Limitações

- O deslizamento é tratado implicitamente por ordens a mercado; não há um parâmetro de desvio separado como no MetaTrader.
- O multiplicador de distância mínima não lê spreads dinâmicos; aplica um amortecedor estático expresso em passos de preço.
- A estratégia assume que apenas um instrumento/segurança é negociado por instância.

## Primeiros passos

1. Configurar os parâmetros da estratégia no Designer ou via código (horários de abertura/fechamento, direção, volume, distâncias de risco).
2. Anexar a estratégia ao instrumento e à fonte de dados desejados.
3. Garantir que a série de velas use o mesmo fuso horário que o calendário pretendido.
4. Executar a estratégia e monitorar o registro de negociações; sobreposições visuais podem ser habilitadas via `DrawCandles` e `DrawOwnTrades` se desejado.

A lógica está completamente contida em `CS/TimeEaStrategy.cs` com extensos comentários em linha explicando cada estágio do fluxo de trabalho.
