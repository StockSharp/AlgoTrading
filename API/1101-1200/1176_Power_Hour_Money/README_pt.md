# Estratégia Power Hour Money
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera durante sessões selecionadas de Nova York e abre posições quando todos os principais períodos concordam.
Uma posição comprada é aberta quando as velas de mês, semana, dia e hora fecham acima de sua abertura.
Uma posição vendida é aberta quando todas fecham abaixo.
Trailing stops opcionais protegem os lucros e as posições podem ser fechadas às 16:45.

## Detalhes

- **Entrada**: comprado quando todos os períodos são verdes, vendido quando todos são vermelhos.
- **Filtro de sessão**: NY 9:30-11:30, estendida 8:00-16:00 ou todas as sessões.
- **Trailing stop**: percentual para os lados comprado e vendido.
- **Fim do dia**: fechamento opcional de todas as posições às 16:45.

## Cálculo e execução

C# e Python usam o mesmo modelo causal: após cada vela base concluída, seu fechamento é o fechamento conhecido das velas em formação de mês, semana, dia e hora. Ele é comparado estritamente com cada abertura. Cores diferentes ou igualdade mantêm a posição. Os fechamentos finais futuros dos períodos maiores nunca são consultados.

Mês, semana iniciada na segunda-feira e dia seguem o calendário de Nova York, incluindo horário de verão. As horas são identificadas em UTC para distinguir a hora repetida no outono. Depois de observar uma mudança de período, a primeira vela base disponível fornece a abertura. Um período já iniciado quando começa o histórico permanece desconhecido até seu próximo limite; o primeiro preço observado não substitui a abertura ausente. As quatro aberturas precisam ser conhecidas antes da entrada.

`CandleType` padrão: cinco minutos. `SessionStart=09:30` e `SessionEnd=11:30` usam tempo de mercado de Nova York, início incluído e fim excluído. Configure `08:00`–`16:00` para a sessão estendida ou `IsAllSessions=true`. `IsAllSessions`, `IsTrailingEnabled` e `IsEndOfDayCloseEnabled` são `false` por padrão; `LongTrailingPercent=2`, `ShortTrailingPercent=2`, `EndOfDayTime=16:45`.

A concordância entra com `Volume` quando sem posição ou reverte completamente com `Volume + abs(Position)`. Uma ordem pendente bloqueia novas ordens; não há intervalo obrigatório. O trailing opcional começa na execução real: fecha compras quando o bid atual atinge `maior bid observado * (1 - LongTrailingPercent / 100)` e vendas quando o ask atinge `menor ask observado * (1 + ShortTrailingPercent / 100)`. Preços executados também atualizam o extremo favorável. Ambos os percentuais devem estar estritamente entre 0 e 100. A reversão reinicia o extremo.

O fechamento diário opcional envia uma ordem a mercado por toda a posição no primeiro evento de tempo de mercado a partir do limite e bloqueia entradas até a meia-noite de Nova York. Cotações e um temporizador de um minuto verificam o limite independentemente das sessões e da formação das velas. Essas saídas exigem eventos de mercado e não garantem preço exato em lacunas.
