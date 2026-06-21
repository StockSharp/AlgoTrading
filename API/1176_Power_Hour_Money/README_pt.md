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
