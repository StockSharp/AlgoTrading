# Estratégia DCA com Trailing Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando uma EMA rápida cruza acima de uma EMA lenta. Até duas ordens de segurança são colocadas quando o preço cai por limiares baseados em ATR ou percentual. As posições são protegidas por um trailing stop padrão e um trailing stop secundário de bloqueio habilitado após um limiar de lucro.

## Parâmetros
- Tipo de vela
- Comprimento da EMA rápida
- Comprimento da EMA lenta
- Usar filtro de data
- Data de início
- Usar espaçamento ATR
- Comprimento do ATR
- Multiplicador ATR para OS1
- Multiplicador ATR para OS2
- Percentual alternativo OS1
- Percentual alternativo OS2
- Barras de cooldown
- Tamanho da ordem base USD
- Tamanho da ordem de segurança 1 USD
- Tamanho da ordem de segurança 2 USD
- Percentual de trailing stop
- Percentual de ativação de bloqueio
- Percentual de trailing de bloqueio
